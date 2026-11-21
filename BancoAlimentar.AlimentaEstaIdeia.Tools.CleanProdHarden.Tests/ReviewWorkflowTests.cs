// -----------------------------------------------------------------------
// <copyright file="ReviewWorkflowTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Execution;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting;
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
