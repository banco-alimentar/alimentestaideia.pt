// -----------------------------------------------------------------------
// <copyright file="BatchSelection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;

    /// <summary>
    /// Builds batch recommendations without replacing decisions that require individual review.
    /// </summary>
    internal static class BatchSelection
    {
        /// <summary>
        /// Filters groups by pattern and exact confidence, retaining the existing all-values behavior.
        /// </summary>
        /// <param name="groups">Available groups.</param>
        /// <param name="patternFilter">Optional pattern.</param>
        /// <param name="confidenceFilter">Optional confidence.</param>
        /// <returns>The matching groups.</returns>
        internal static IEnumerable<AnalyzedGroup> Filter(IEnumerable<AnalyzedGroup> groups, string? patternFilter, string? confidenceFilter)
        {
            if (Enum.TryParse(patternFilter, out GroupPattern pattern))
            {
                groups = groups.Where(group => group.Recommendation.Pattern == pattern);
            }

            if (Enum.TryParse(confidenceFilter, out RecommendationConfidence confidence))
            {
                groups = groups.Where(group => group.Recommendation.Confidence == confidence);
            }

            return groups;
        }

        /// <summary>
        /// Explains why a group cannot be selected, or returns null when it is eligible.
        /// </summary>
        /// <param name="group">Reviewed evidence.</param>
        /// <param name="record">Existing decision.</param>
        /// <returns>The selection blocker.</returns>
        internal static string? Blocker(AnalyzedGroup group, GroupDecisionRecord? record)
        {
            GroupRecommendation recommendation = group.Recommendation;
            if (recommendation.Action == null || string.IsNullOrWhiteSpace(recommendation.SuggestedReason))
            {
                return "No complete recommendation; review this group individually.";
            }

            if (recommendation.Action == ReconciliationAction.ReconcileIdentifiers
                && !group.Evaluation.Members.Any(member => member.PaymentId == recommendation.CanonicalPaymentId))
            {
                return "No recommended owner in this group; review it individually.";
            }

            if (record != null)
            {
                if (record.Status is DecisionStatus.Applied or DecisionStatus.Deferred)
                {
                    return "Applied or deferred decisions cannot be selected.";
                }

                if (record.EvidenceHash != group.EvidenceHash
                    || record.Action != recommendation.Action
                    || record.CanonicalPaymentId != recommendation.CanonicalPaymentId
                    || !string.Equals(record.Reason.Trim(), recommendation.SuggestedReason.Trim(), StringComparison.Ordinal))
                {
                    return "The saved decision differs from the recommendation; review it individually.";
                }
            }

            return null;
        }

        /// <summary>
        /// Builds the exact recommended decision, retaining private notes.
        /// </summary>
        /// <param name="group">Reviewed evidence.</param>
        /// <param name="approvedBy">Reviewer.</param>
        /// <param name="record">Existing decision.</param>
        /// <returns>The decision.</returns>
        internal static DecisionInput Input(AnalyzedGroup group, string approvedBy, GroupDecisionRecord? record)
        {
            string? blocker = Blocker(group, record);
            if (blocker != null)
            {
                throw new InvalidOperationException($"Group {group.GroupId}: {blocker}");
            }

            return new DecisionInput(
                group.GroupId,
                group.Evaluation.EasyPayPaymentId,
                group.EvidenceHash,
                group.Recommendation.Action,
                group.Recommendation.CanonicalPaymentId,
                group.Recommendation.SuggestedReason,
                approvedBy,
                record?.Notes);
        }
    }
}
