// -----------------------------------------------------------------------
// <copyright file="SqlServerDryRunTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution;
    using Microsoft.Extensions.Logging.Abstractions;
    using Xunit;

    /// <summary>
    /// Runs the executor against a real SQL Server copy. Nothing is committed: these tests only dry run,
    /// or try an apply that the executor must reject before it changes anything.
    /// Set <c>CLEANPRODHARDEN_TEST_SQL</c> to a connection string to run them.
    /// </summary>
    public class SqlServerDryRunTests
    {
        private const string ConnectionVariable = "CLEANPRODHARDEN_TEST_SQL";

        /// <summary>
        /// The recommended action for one group of each pattern passes a dry run, and the evidence is unchanged afterwards.
        /// </summary>
        [SkippableFact]
        public async Task RecommendedActionsPassDryRunWithoutChangingData()
        {
            (SqlServerTargetFactory factory, IReadOnlyList<AnalyzedGroup> groups) = await LoadAsync();
            var executor = new GroupActionExecutor(factory, NullLogger<GroupActionExecutor>.Instance);
            List<AnalyzedGroup> samples = groups
                .Where(group => group.Recommendation.Action.HasValue)
                .GroupBy(group => group.Recommendation.Pattern)
                .Select(pattern => pattern.First())
                .ToList();
            Skip.If(samples.Count == 0, "No group has a recommended action.");

            foreach (AnalyzedGroup group in samples)
            {
                ExecutionResult result = await executor.ExecuteAsync(Request(group, dryRun: true, group.EvidenceHash, confirmDatabaseName: null), CancellationToken.None);

                Assert.True(result.Succeeded, $"Group {group.GroupId}: " + string.Join("; ", result.Issues.Select(issue => $"[{issue.Code}] {issue.Message}")));
                Assert.False(result.Committed);
            }

            await using ApplicationDbContext db = factory.CreateDbContext();
            IReadOnlyDictionary<int, string> after = await DuplicateGroupLoader.LoadEvidenceHashesAsync(db, null, CancellationToken.None);
            Assert.Equal(groups.ToDictionary(group => group.GroupId, group => group.EvidenceHash), after);
        }

        /// <summary>
        /// A decision reviewed on older evidence is rejected (51007).
        /// </summary>
        [SkippableFact]
        public async Task StaleEvidenceIsRejected()
        {
            (SqlServerTargetFactory factory, IReadOnlyList<AnalyzedGroup> groups) = await LoadAsync();
            AnalyzedGroup group = groups.First(candidate => candidate.Recommendation.Action.HasValue);
            var executor = new GroupActionExecutor(factory, NullLogger<GroupActionExecutor>.Instance);

            ExecutionResult result = await executor.ExecuteAsync(Request(group, dryRun: true, "STALE", confirmDatabaseName: null), CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Contains(result.Issues, issue => issue.Code == ValidationCodes.StaleEvidence);
        }

        /// <summary>
        /// An apply without the typed database name is rejected before anything changes (51100).
        /// </summary>
        [SkippableFact]
        public async Task ApplyWithoutTypedDatabaseNameIsRejected()
        {
            (SqlServerTargetFactory factory, IReadOnlyList<AnalyzedGroup> groups) = await LoadAsync();
            AnalyzedGroup group = groups.First(candidate => candidate.Recommendation.Action.HasValue);
            var executor = new GroupActionExecutor(factory, NullLogger<GroupActionExecutor>.Instance);

            ExecutionResult result = await executor.ExecuteAsync(Request(group, dryRun: false, group.EvidenceHash, "__not-the-target__"), CancellationToken.None);

            Assert.False(result.Committed);
            Assert.Equal(ValidationCodes.TargetNotConfirmed, Assert.Single(result.Issues).Code);
        }

        private static async Task<(SqlServerTargetFactory Factory, IReadOnlyList<AnalyzedGroup> Groups)> LoadAsync()
        {
            string? connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
            Skip.If(string.IsNullOrWhiteSpace(connectionString), $"Set {ConnectionVariable} to a SQL Server connection string to run this test.");
            var factory = new SqlServerTargetFactory(connectionString!);
            await using ApplicationDbContext db = factory.CreateDbContext();
            GroupAnalyzer.AnalysisResult analysis = await GroupAnalyzer.AnalyzeAsync(db, null, false, CancellationToken.None);
            Skip.If(analysis.Groups.Count == 0, "The database has no duplicate groups.");
            return (factory, analysis.Groups);
        }

        private static ExecutionRequest Request(AnalyzedGroup group, bool dryRun, string evidenceHash, string? confirmDatabaseName)
        {
            GroupRecommendation recommendation = group.Recommendation;
            return new ExecutionRequest(group.GroupId, recommendation.Action, recommendation.CanonicalPaymentId, recommendation.SuggestedReason, "CleanProdHarden tests", evidenceHash, dryRun, confirmDatabaseName);
        }
    }
}
