// -----------------------------------------------------------------------
// <copyright file="GroupAnalyzer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// Evaluates groups and attaches evidence hashes, environment checks, and recommendations.
    /// </summary>
    internal static class GroupAnalyzer
    {
        /// <summary>
        /// Analyzes every group, or one group.
        /// </summary>
        /// <param name="db">Context. Pass a context with an open transaction to analyze under locks.</param>
        /// <param name="groupId">Optional group filter.</param>
        /// <param name="lockDependencies">Take table locks on referencing tables.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The analysis.</returns>
        public static async Task<AnalysisResult> AnalyzeAsync(ApplicationDbContext db, int? groupId, bool lockDependencies, CancellationToken cancellationToken)
        {
            IReadOnlyList<GroupEvaluation> evaluations = await DuplicateGroupLoader.LoadEvaluationsAsync(db, groupId, cancellationToken);
            IReadOnlyDictionary<int, string> hashes = await DuplicateGroupLoader.LoadEvidenceHashesAsync(db, groupId, cancellationToken);
            (HashSet<int> invoiced, Dictionary<int, int> subscriptions) = await DuplicateGroupLoader.LoadDonationContextAsync(db, evaluations, cancellationToken);
            List<int> paymentIds = evaluations.SelectMany(group => group.Members).Select(member => member.PaymentId).ToList();
            ExecutionEnvironment shared = await DuplicateGroupLoader.LoadEnvironmentAsync(db, paymentIds, string.Empty, lockDependencies, cancellationToken);

            var warnings = new List<string>();
            var groups = new List<AnalyzedGroup>();
            var environments = new Dictionary<int, ExecutionEnvironment>();
            foreach (GroupEvaluation evaluation in evaluations.OrderBy(group => group.GroupId))
            {
                if (!hashes.TryGetValue(evaluation.GroupId, out string? hash))
                {
                    warnings.Add($"Group {evaluation.GroupId} has no SQL evidence hash. The model and SQL disagree on group membership; it cannot be applied.");
                    hash = string.Empty;
                }

                ExecutionEnvironment environment = shared with { CurrentEvidenceHash = hash };
                bool environmentAllowsDeletion = environment.AllowsDeletion(evaluation);
                var context = new GroupContext
                {
                    DonationIdsWithInvoice = invoiced,
                    SubscriptionIdByDonationId = subscriptions,
                    CanDeleteGroup = environmentAllowsDeletion,
                };
                GroupRecommendation recommendation = RecommendationEngine.Recommend(evaluation, context);
                groups.Add(new AnalyzedGroup(evaluation, hash, context, recommendation, evaluation.HasSafeDeletionEvidence && environmentAllowsDeletion));
                environments[evaluation.GroupId] = environment;
            }

            foreach (int missing in hashes.Keys.Except(evaluations.Select(group => group.GroupId)).OrderBy(id => id))
            {
                warnings.Add($"SQL reports duplicate group {missing}, but the model could not load it. Check for EasyPay payment types the model does not map.");
            }

            if (!shared.DependencyCheckSupported)
            {
                warnings.Add("Dependency checks are not reliable (missing VIEW DEFINITION permission or unsupported foreign keys). DeleteGroup is disabled.");
            }

            return new AnalysisResult(groups, environments, warnings);
        }

        /// <summary>
        /// Analysis output.
        /// </summary>
        /// <param name="Groups">Groups ordered by ID.</param>
        /// <param name="Environments">Environment by group ID.</param>
        /// <param name="Warnings">Consistency warnings.</param>
        internal sealed record AnalysisResult(
            IReadOnlyList<AnalyzedGroup> Groups,
            IReadOnlyDictionary<int, ExecutionEnvironment> Environments,
            IReadOnlyList<string> Warnings);
    }
}
