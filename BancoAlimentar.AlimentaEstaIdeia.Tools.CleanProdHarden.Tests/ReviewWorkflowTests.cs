// -----------------------------------------------------------------------
// <copyright file="ReviewWorkflowTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Components;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Components.Shared;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Xunit;

    /// <summary>
    /// Tests the decision store and the save, dry run, apply order.
    /// </summary>
    public sealed class ReviewWorkflowTests : IDisposable
    {
        private static readonly TargetIdentity Target = new TargetIdentity("server1", "prod", "sa", "17.0");

        private readonly InMemoryDecisionStoreFactory storeFactory = new InMemoryDecisionStoreFactory();
        private readonly DecisionStore store;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewWorkflowTests"/> class.
        /// </summary>
        public ReviewWorkflowTests()
        {
            this.store = new DecisionStore(this.storeFactory);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.storeFactory.Dispose();
        }

        /// <summary>
        /// Saving creates a draft, deferring marks it deferred, and both are recorded in history.
        /// </summary>
        [Fact]
        public async Task SaveAndDeferAreRecorded()
        {
            DecisionInput input = Input();

            GroupDecisionRecord saved = await this.store.SaveAsync(Target.Key, input, defer: false, CancellationToken.None);
            GroupDecisionRecord deferred = await this.store.SaveAsync(Target.Key, input, defer: true, CancellationToken.None);

            Assert.Equal(DecisionStatus.Draft, saved.Status);
            Assert.Equal(DecisionStatus.Deferred, deferred.Status);
            var events = await this.store.ListEventsAsync(Target.Key, input.GroupId, 10, CancellationToken.None);
            Assert.Equal(new[] { DecisionEventKind.Deferred, DecisionEventKind.Saved }, events.Select(record => record.Kind));
        }

        /// <summary>
        /// Decisions are kept per target, so a copy of the database does not share decisions with production.
        /// </summary>
        [Fact]
        public async Task DecisionsAreSeparatedByTarget()
        {
            await this.store.SaveAsync(Target.Key, Input(), defer: false, CancellationToken.None);

            Assert.Single(await this.store.ListAsync(Target.Key, CancellationToken.None));
            Assert.Empty(await this.store.ListAsync("server1|prod_copy", CancellationToken.None));
        }

        /// <summary>
        /// A passing dry run survives a re-save of the same decision, but any change resets it to draft.
        /// </summary>
        [Fact]
        public async Task ChangingDecisionResetsDryRun()
        {
            DecisionInput input = Input();
            await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: true, succeeded: true), CancellationToken.None);

            GroupDecisionRecord same = await this.store.SaveAsync(Target.Key, input with { Notes = "only notes changed" }, defer: false, CancellationToken.None);
            GroupDecisionRecord changed = await this.store.SaveAsync(Target.Key, input with { Reason = "A different reason." }, defer: false, CancellationToken.None);

            Assert.Equal(DecisionStatus.DryRunPassed, same.Status);
            Assert.Equal(DecisionStatus.Draft, changed.Status);
        }

        /// <summary>
        /// A failed dry run clears the fingerprint.
        /// </summary>
        [Fact]
        public async Task FailedDryRunClearsFingerprint()
        {
            DecisionInput input = Input();

            GroupDecisionRecord record = await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: true, succeeded: false), CancellationToken.None);

            Assert.Equal(DecisionStatus.DryRunFailed, record.Status);
            Assert.Null(record.DryRunFingerprint);
            Assert.Equal("[51007] stale", record.LastIssues);
        }

        /// <summary>
        /// An applied decision is final.
        /// </summary>
        [Fact]
        public async Task AppliedDecisionIsFinal()
        {
            DecisionInput input = Input();
            GroupDecisionRecord applied = await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: false, succeeded: true), CancellationToken.None);

            Assert.Equal(DecisionStatus.Applied, applied.Status);
            Assert.Equal("PLANHASH", applied.PlanEvidenceHash);
            await Assert.ThrowsAsync<InvalidOperationException>(() => this.store.SaveAsync(Target.Key, input, defer: false, CancellationToken.None));
        }

        /// <summary>
        /// Apply is rejected when it is disabled in configuration (51104).
        /// </summary>
        [Fact]
        public async Task ApplyIsRejectedWhenDisabled()
        {
            DecisionInput input = Input();
            await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: true, succeeded: true), CancellationToken.None);

            ExecutionResult result = await this.Workflow(allowApply: false).ApplyAsync(Target, input, "prod", CancellationToken.None);

            Assert.False(result.Committed);
            Assert.Equal(ValidationCodes.ApplyDisabled, Assert.Single(result.Issues).Code);
        }

        /// <summary>
        /// Apply is rejected without a passing dry run of the exact same decision and evidence (51103).
        /// </summary>
        [Fact]
        public async Task ApplyRequiresMatchingDryRun()
        {
            DecisionInput input = Input();
            ReviewWorkflow workflow = this.Workflow(allowApply: true);

            ExecutionResult noDryRun = await workflow.ApplyAsync(Target, input, "prod", CancellationToken.None);
            await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: true, succeeded: true), CancellationToken.None);
            ExecutionResult otherOwner = await workflow.ApplyAsync(Target, input with { Action = ReconciliationAction.ReconcileIdentifiers, CanonicalPaymentId = 300 }, "prod", CancellationToken.None);
            ExecutionResult otherReviewer = await workflow.ApplyAsync(Target, input with { ApprovedBy = "Someone Else" }, "prod", CancellationToken.None);
            ExecutionResult newEvidence = await workflow.ApplyAsync(Target, input with { EvidenceHash = "NEWHASH" }, "prod", CancellationToken.None);
            ExecutionResult otherTarget = await workflow.ApplyAsync(Target with { DatabaseName = "prod_copy" }, input, "prod_copy", CancellationToken.None);

            Assert.All(
                new[] { noDryRun, otherOwner, otherReviewer, newEvidence, otherTarget },
                result => Assert.Equal(ValidationCodes.DryRunRequired, Assert.Single(result.Issues).Code));
        }

        /// <summary>
        /// The dry-run fingerprint covers the target and everything written to the database, but not private notes.
        /// </summary>
        [Fact]
        public void FingerprintCoversWhatTheApplyWrites()
        {
            DecisionInput input = Input();
            string fingerprint = input.Fingerprint(Target.Key);

            Assert.Equal(fingerprint, (input with { Notes = "private" }).Fingerprint(Target.Key));
            Assert.Equal(fingerprint, (input with { Reason = "  " + input.Reason + " " }).Fingerprint(Target.Key));
            Assert.NotEqual(fingerprint, input.Fingerprint("server1|prod_copy"));
            Assert.NotEqual(fingerprint, (input with { Action = ReconciliationAction.DeleteGroup }).Fingerprint(Target.Key));
            Assert.NotEqual(fingerprint, (input with { EvidenceHash = "OTHER" }).Fingerprint(Target.Key));
        }

        /// <summary>
        /// The plan hash binds a group to its server, database, and evidence, like the SQL script.
        /// </summary>
        [Fact]
        public void PlanHashBindsTargetAndEvidence()
        {
            string hash = GroupActionExecutor.ComputePlanHash(Target, 300, "EVIDENCE");

            Assert.Matches("^[0-9A-F]{64}$", hash);
            Assert.Equal(hash, GroupActionExecutor.ComputePlanHash(Target, 300, "EVIDENCE"));
            Assert.NotEqual(hash, GroupActionExecutor.ComputePlanHash(Target with { ServerName = "server2" }, 300, "EVIDENCE"));
            Assert.NotEqual(hash, GroupActionExecutor.ComputePlanHash(Target with { DatabaseName = "prod_copy" }, 300, "EVIDENCE"));
            Assert.NotEqual(hash, GroupActionExecutor.ComputePlanHash(Target, 301, "EVIDENCE"));
            Assert.NotEqual(hash, GroupActionExecutor.ComputePlanHash(Target, 300, "CHANGED"));
        }

        /// <summary>
        /// Mixed decisions run in order and stop at the first failure, retaining previous commits.
        /// </summary>
        /// <param name="dryRun">Whether the batch rolls back.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BatchStopsAtFirstFailure(bool dryRun)
        {
            DecisionInput[] inputs =
            {
                Input() with { Action = ReconciliationAction.ReconcileIdentifiers, CanonicalPaymentId = 300 },
                Input() with { GroupId = 400 },
                Input() with { GroupId = 500 },
            };
            var order = new List<string>();

            IReadOnlyList<ExecutionResult> results = await ReviewWorkflow.RunBatchAsync(
                inputs,
                async input =>
                {
                    order.Add($"start {input.GroupId}");
                    await Task.Yield();
                    return Result(input, dryRun, succeeded: input.GroupId != 400);
                },
                result =>
                {
                    order.Add($"complete {result.Request.GroupId}");
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            Assert.Equal(new[] { "start 300", "complete 300", "start 400", "complete 400" }, order);
            Assert.Equal(2, results.Count);
            Assert.True(results[0].Succeeded);
            Assert.Equal(!dryRun, results[0].Committed);
            Assert.Equal(ReconciliationAction.ReconcileIdentifiers, results[0].Request.Action);
            Assert.Equal(ReconciliationAction.RetireIdentifier, results[1].Request.Action);
            Assert.False(results[1].Committed);
        }

        /// <summary>
        /// Cancellation between groups never starts the remaining selections.
        /// </summary>
        [Fact]
        public async Task BatchCancellationStopsRemainingGroups()
        {
            using var cancellation = new CancellationTokenSource();
            var started = new List<int>();
            var completed = new List<ExecutionResult>();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReviewWorkflow.RunBatchAsync(
                new[] { Input(), Input() with { GroupId = 400 } },
                input =>
                {
                    started.Add(input.GroupId);
                    return Task.FromResult(Result(input, dryRun: false, succeeded: true));
                },
                result =>
                {
                    completed.Add(result);
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                cancellation.Token));

            Assert.Equal(new[] { 300 }, started);
            Assert.True(Assert.Single(completed).Committed);
        }

        /// <summary>
        /// A missing dry run on a later group blocks the entire batch before any target access.
        /// </summary>
        [Fact]
        public async Task BatchApplyPreflightsEveryDecision()
        {
            DecisionInput first = Input();
            DecisionInput second = Input() with { GroupId = 400 };
            await this.store.RecordRunAsync(Target.Key, first, Result(first, dryRun: true, succeeded: true), CancellationToken.None);
            var reported = new List<ExecutionResult>();

            IReadOnlyList<ExecutionResult> results = await this.Workflow(allowApply: true).ApplyBatchAsync(
                Target,
                new[] { first, second },
                "prod",
                result =>
                {
                    reported.Add(result);
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            ExecutionResult rejected = Assert.Single(results);
            Assert.Equal(400, rejected.Request.GroupId);
            Assert.Equal(ValidationCodes.DryRunRequired, Assert.Single(rejected.Issues).Code);
            Assert.Equal(rejected, Assert.Single(reported));
            Assert.False(rejected.Committed);
            Assert.Equal(DecisionStatus.DryRunPassed, (await this.store.GetAsync(Target.Key, first.GroupId, CancellationToken.None))?.Status);
            Assert.All(await this.store.ListEventsAsync(Target.Key, null, 10, CancellationToken.None), record => Assert.Equal(DecisionEventKind.DryRun, record.Kind));
        }

        /// <summary>
        /// Batch applies preserve the apply-enable and typed-target gates.
        /// </summary>
        /// <param name="allowApply">Whether apply is configured.</param>
        /// <param name="confirmation">Typed database name.</param>
        /// <param name="expectedCode">Expected rejection.</param>
        [Theory]
        [InlineData(false, "prod", ValidationCodes.ApplyDisabled)]
        [InlineData(true, "prod_copy", ValidationCodes.TargetNotConfirmed)]
        public async Task BatchApplyRequiresEnableAndConfirmation(bool allowApply, string confirmation, int expectedCode)
        {
            DecisionInput input = Input();
            await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: true, succeeded: true), CancellationToken.None);

            IReadOnlyList<ExecutionResult> results = await this.Workflow(allowApply).ApplyBatchAsync(
                Target, new[] { input }, confirmation, _ => Task.CompletedTask, CancellationToken.None);

            Assert.Equal(expectedCode, Assert.Single(Assert.Single(results).Issues).Code);
        }

        /// <summary>
        /// Changing the reviewer or evidence of any group invalidates a batch approval.
        /// </summary>
        [Fact]
        public async Task BatchApplyRejectsChangedDecisionAndTarget()
        {
            DecisionInput input = Input();
            await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: true, succeeded: true), CancellationToken.None);
            ReviewWorkflow workflow = this.Workflow(allowApply: true);

            foreach (DecisionInput changed in new[] { input with { ApprovedBy = "Other reviewer" }, input with { EvidenceHash = "NEW" } })
            {
                var results = await workflow.ApplyBatchAsync(Target, new[] { changed }, "prod", _ => Task.CompletedTask, CancellationToken.None);
                Assert.Equal(ValidationCodes.DryRunRequired, Assert.Single(Assert.Single(results).Issues).Code);
            }

            var otherTarget = Target with { ServerName = "other_server" };
            var otherResults = await workflow.ApplyBatchAsync(otherTarget, new[] { input }, "prod", _ => Task.CompletedTask, CancellationToken.None);
            Assert.Equal(ValidationCodes.DryRunRequired, Assert.Single(Assert.Single(otherResults).Issues).Code);
        }

        /// <summary>
        /// Batch dry runs never overwrite applied or deferred decisions.
        /// </summary>
        /// <param name="applied">Whether the protected decision is applied rather than deferred.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BatchDryRunPreservesProtectedDecisions(bool applied)
        {
            DecisionInput input = Input();
            if (applied)
            {
                await this.store.RecordRunAsync(Target.Key, input, Result(input, dryRun: false, succeeded: true), CancellationToken.None);
            }
            else
            {
                await this.store.SaveAsync(Target.Key, input, defer: true, CancellationToken.None);
            }

            await Assert.ThrowsAsync<InvalidOperationException>(() => this.Workflow(allowApply: true).DryRunBatchAsync(
                Target, new[] { Input() with { GroupId = 400 }, input }, _ => Task.CompletedTask, CancellationToken.None));

            Assert.Equal(applied ? DecisionStatus.Applied : DecisionStatus.Deferred, (await this.store.GetAsync(Target.Key, input.GroupId, CancellationToken.None))?.Status);
            Assert.Single(await this.store.ListAsync(Target.Key, CancellationToken.None));
        }

        /// <summary>
        /// Empty and duplicate selections are rejected before executing any group.
        /// </summary>
        [Fact]
        public async Task BatchRejectsEmptyOrDuplicateSelections()
        {
            Task<ExecutionResult> UnexpectedRun(DecisionInput input) => throw new InvalidOperationException("A group should not start.");
            await Assert.ThrowsAsync<ArgumentException>(() => ReviewWorkflow.RunBatchAsync(
                Array.Empty<DecisionInput>(), UnexpectedRun, _ => Task.CompletedTask, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentException>(() => ReviewWorkflow.RunBatchAsync(
                new[] { Input(), Input() }, UnexpectedRun, _ => Task.CompletedTask, CancellationToken.None));
        }

        /// <summary>
        /// The batch UI previews each selected decision and never exposes apply without approval.
        /// </summary>
        [Fact]
        public async Task BatchPreviewShowsIndividualRecommendations()
        {
            GroupEvaluation first = GroupScenarios.ReconciliationToolCopy();
            GroupEvaluation second = GroupScenarios.SupersededAttempts();
            AnalyzedGroup[] groups =
            {
                new AnalyzedGroup(first, "HASH1", GroupContext.Empty, RecommendationEngine.Recommend(first, GroupContext.Empty), false),
                new AnalyzedGroup(second, "HASH2", GroupContext.Empty, RecommendationEngine.Recommend(second, GroupContext.Empty), false),
            };
            await using ServiceProvider services = new ServiceCollection()
                .AddLogging()
                .AddSingleton(this.Workflow(allowApply: true))
                .AddSingleton(new ReviewerSession())
                .BuildServiceProvider();
            await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

            string html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(BatchReview.Groups)] = groups,
                    [nameof(BatchReview.Target)] = Target,
                });
                var rendered = await renderer.RenderComponentAsync<BatchReview>(parameters);
                return WebUtility.HtmlDecode(rendered.ToHtmlString());
            });

            Assert.Contains("Batch reconciliation (2 selected)", html);
            Assert.Contains("groups/100", html);
            Assert.Contains("groups/300", html);
            Assert.Contains("102", html);
            Assert.All(groups, group =>
            {
                Assert.Contains(Display.ActionLabel(group.Recommendation.Action), html);
                Assert.Contains(group.Recommendation.SuggestedReason, html);
            });
            Assert.Contains("Dry run selected groups", html);
            Assert.DoesNotContain("Apply selected groups", html);
            Assert.Contains("Every selected group must pass a dry run", html);
        }

        private static DecisionInput Input()
        {
            return new DecisionInput(300, GroupScenarios.ProviderId, "HASH", ReconciliationAction.RetireIdentifier, null, "Superseded attempts: retire.", "Test Reviewer", null);
        }

        private static ExecutionResult Result(DecisionInput input, bool dryRun, bool succeeded)
        {
            return new ExecutionResult
            {
                Request = new ExecutionRequest(input.GroupId, input.Action, input.CanonicalPaymentId, input.Reason, input.ApprovedBy, input.EvidenceHash, dryRun, dryRun ? null : "prod"),
                Succeeded = succeeded,
                Committed = succeeded && !dryRun,
                PlanEvidenceHash = succeeded && !dryRun ? "PLANHASH" : null,
                Issues = succeeded ? Array.Empty<ValidationIssue>() : new[] { new ValidationIssue(ValidationCodes.StaleEvidence, "stale") },
                AffectedPaymentIds = new[] { 300, 301 },
            };
        }

        private ReviewWorkflow Workflow(bool allowApply)
        {
            var target = new UnreachableTargetFactory();
            return new ReviewWorkflow(
                new GroupActionExecutor(target, NullLogger<GroupActionExecutor>.Instance),
                this.store,
                new DuplicateGroupService(target),
                Options.Create(new CleanProdHardenOptions { AllowApply = allowApply }));
        }
    }
}
