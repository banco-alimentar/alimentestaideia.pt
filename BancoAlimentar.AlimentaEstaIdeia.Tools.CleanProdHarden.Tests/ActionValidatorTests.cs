// -----------------------------------------------------------------------
// <copyright file="ActionValidatorTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using Xunit;

    /// <summary>
    /// Tests that the validator blocks the same decisions as the SQL script, with the same codes.
    /// </summary>
    public class ActionValidatorTests
    {
        private const string Hash = "ABC123";

        private static readonly ExecutionEnvironment Clean = new ExecutionEnvironment
        {
            CurrentEvidenceHash = Hash,
            HasViewDefinitionPermission = true,
        };

        /// <summary>
        /// A complete, allowed decision has no issues.
        /// </summary>
        [Fact]
        public void ValidDecisionsPass()
        {
            Assert.Empty(Validate(GroupScenarios.ReconciliationToolCopy(), ReconciliationAction.ReconcileIdentifiers, 102));
            Assert.Empty(Validate(GroupScenarios.SupersededAttempts(), ReconciliationAction.RetireIdentifier, null));
            Assert.Empty(Validate(GroupScenarios.SupersededAttempts(), ReconciliationAction.DeleteGroup, null));
        }

        /// <summary>
        /// The reviewer name is required (51006).
        /// </summary>
        [Fact]
        public void ReviewerIsRequired()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();
            var decision = new ProposedDecision(group.GroupId, ReconciliationAction.RetireIdentifier, null, "reason", "  ", Hash);

            Assert.Contains(ValidationCodes.ApprovalMissing, Codes(ActionValidator.Validate(group, decision, Clean)));
        }

        /// <summary>
        /// A changed evidence hash blocks the run (51007).
        /// </summary>
        [Fact]
        public void StaleEvidenceIsBlocked()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();
            ExecutionEnvironment changed = Clean with { CurrentEvidenceHash = "DIFFERENT" };

            Assert.Contains(ValidationCodes.StaleEvidence, Codes(Validate(group, ReconciliationAction.RetireIdentifier, null, changed)));
        }

        /// <summary>
        /// A missing action is blocked (51010).
        /// </summary>
        [Fact]
        public void MissingActionIsBlocked()
        {
            Assert.Contains(ValidationCodes.UnsupportedAction, Codes(Validate(GroupScenarios.SupersededAttempts(), null, null)));
        }

        /// <summary>
        /// Every action needs a reason (51002).
        /// </summary>
        [Fact]
        public void ReasonIsRequired()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();
            var decision = new ProposedDecision(group.GroupId, ReconciliationAction.RetireIdentifier, null, " ", "Reviewer", Hash);

            Assert.Contains(ValidationCodes.ReasonOrOwner, Codes(ActionValidator.Validate(group, decision, Clean)));
        }

        /// <summary>
        /// Reconcile needs an owner that belongs to the group (51002, 51008); other actions take no owner.
        /// </summary>
        [Fact]
        public void OwnerRulesAreEnforced()
        {
            GroupEvaluation group = GroupScenarios.ReconciliationToolCopy();

            Assert.Contains(ValidationCodes.ReasonOrOwner, Codes(Validate(group, ReconciliationAction.ReconcileIdentifiers, null)));
            Assert.Contains(ValidationCodes.OwnerNotMember, Codes(Validate(group, ReconciliationAction.ReconcileIdentifiers, 999)));
            Assert.Contains(ValidationCodes.ReasonOrOwner, Codes(Validate(GroupScenarios.SupersededAttempts(), ReconciliationAction.RetireIdentifier, 300)));
        }

        /// <summary>
        /// Retire is blocked while a member is confirmed (51013).
        /// </summary>
        [Fact]
        public void RetireWithConfirmedMemberIsBlocked()
        {
            Assert.Contains(ValidationCodes.RetireConfirmed, Codes(Validate(GroupScenarios.ZeroValueRetries(), ReconciliationAction.RetireIdentifier, null)));
        }

        /// <summary>
        /// Delete needs a safe deletion basis for every member (51011).
        /// </summary>
        [Fact]
        public void UnsafeDeletionIsBlocked()
        {
            Assert.Contains(ValidationCodes.UnsafeDeletion, Codes(Validate(GroupScenarios.ZeroValueRetries(), ReconciliationAction.DeleteGroup, null)));
            Assert.Contains(ValidationCodes.UnsafeDeletion, Codes(Validate(GroupScenarios.AbandonedOrphans(paid: null), ReconciliationAction.DeleteGroup, null)));
        }

        /// <summary>
        /// Delete is blocked by dependent rows, triggers, or missing permissions (51012).
        /// </summary>
        [Fact]
        public void DeletionDependenciesAreBlocked()
        {
            GroupEvaluation group = GroupScenarios.SupersededAttempts();
            var dependent = Clean with { Dependencies = new[] { new PaymentDependency(300, "dbo.PaymentItem", "PaymentId", "NO_ACTION", 1) } };
            var unrelated = Clean with { Dependencies = new[] { new PaymentDependency(12345, "dbo.PaymentItem", "PaymentId", "NO_ACTION", 1) } };
            var triggers = Clean with { EnabledPaymentTriggerCount = 1 };
            var noPermission = Clean with { HasViewDefinitionPermission = false };
            var compositeKey = Clean with { UnsupportedForeignKeys = new[] { "FK_Composite" } };

            Assert.Contains(ValidationCodes.DeletionDependencies, Codes(Validate(group, ReconciliationAction.DeleteGroup, null, dependent)));
            Assert.Empty(Validate(group, ReconciliationAction.DeleteGroup, null, unrelated));
            Assert.Contains(ValidationCodes.DeletionDependencies, Codes(Validate(group, ReconciliationAction.DeleteGroup, null, triggers)));
            Assert.Contains(ValidationCodes.DeletionDependencies, Codes(Validate(group, ReconciliationAction.DeleteGroup, null, noPermission)));
            Assert.Contains(ValidationCodes.DeletionDependencies, Codes(Validate(group, ReconciliationAction.DeleteGroup, null, compositeKey)));
            Assert.False(dependent.AllowsDeletion(group));
            Assert.True(unrelated.AllowsDeletion(group));

            // Retire does not delete rows, so dependencies do not block it.
            Assert.Empty(Validate(group, ReconciliationAction.RetireIdentifier, null, dependent));
        }

        /// <summary>
        /// Existing audit rows for an affected member block the action (51003); the owner is not affected.
        /// </summary>
        [Fact]
        public void ExistingAuditHistoryIsBlocked()
        {
            GroupEvaluation group = GroupScenarios.ReconciliationToolCopy();
            var ownerAudited = Clean with { ReconciliationAuditPaymentIds = new HashSet<int> { 102 } };
            var memberAudited = Clean with { ReconciliationAuditPaymentIds = new HashSet<int> { 100 } };

            Assert.Empty(Validate(group, ReconciliationAction.ReconcileIdentifiers, 102, ownerAudited));
            Assert.Contains(ValidationCodes.AuditHistoryExists, Codes(Validate(group, ReconciliationAction.ReconcileIdentifiers, 102, memberAudited)));
        }

        /// <summary>
        /// Other migration violations block every action (51009).
        /// </summary>
        [Fact]
        public void OtherMigrationViolationsAreBlocked()
        {
            var overlength = Clean with { OverlengthProviderIds = 1 };

            Assert.Contains(ValidationCodes.MigrationRequirements, Codes(Validate(GroupScenarios.SupersededAttempts(), ReconciliationAction.RetireIdentifier, null, overlength)));
        }

        /// <summary>
        /// Reconcile affects every member except the owner; other actions affect every member.
        /// </summary>
        [Fact]
        public void AffectedPaymentIdsExcludeOwner()
        {
            GroupEvaluation group = GroupScenarios.ReconciliationToolCopy();

            Assert.Equal(new[] { 100, 101 }, ActionValidator.AffectedPaymentIds(group, ReconciliationAction.ReconcileIdentifiers, 102));
            Assert.Equal(new[] { 100, 101, 102 }, ActionValidator.AffectedPaymentIds(group, ReconciliationAction.RetireIdentifier, null));
        }

        private static IReadOnlyList<ValidationIssue> Validate(GroupEvaluation group, ReconciliationAction? action, int? owner, ExecutionEnvironment? environment = null)
        {
            var decision = new ProposedDecision(group.GroupId, action, owner, "Reviewed in tests.", "Test Reviewer", Hash);
            return ActionValidator.Validate(group, decision, environment ?? Clean);
        }

        private static IEnumerable<int> Codes(IEnumerable<ValidationIssue> issues)
        {
            return issues.Select(issue => issue.Code);
        }
    }
}
