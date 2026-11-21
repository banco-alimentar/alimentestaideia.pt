// -----------------------------------------------------------------------
// <copyright file="ActionValidator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Port of the execution checks in <c>MergeDuplicateEasyPayPaymentIds.sql</c>, applied to one group.
    /// </summary>
    public static class ActionValidator
    {
        /// <summary>
        /// Maximum length of the approver column.
        /// </summary>
        public const int MaxApprovedByLength = 200;

        /// <summary>
        /// Maximum length of the approval reason column.
        /// </summary>
        public const int MaxReasonLength = 2000;

        /// <summary>
        /// Validates a decision against the current group evidence and database environment.
        /// </summary>
        /// <param name="group">Group evaluated inside the execution transaction.</param>
        /// <param name="decision">Reviewer decision.</param>
        /// <param name="environment">Database facts.</param>
        /// <returns>Every blocking issue; empty when the decision can run.</returns>
        public static IReadOnlyList<ValidationIssue> Validate(GroupEvaluation group, ProposedDecision decision, ExecutionEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(decision);
            ArgumentNullException.ThrowIfNull(environment);

            var issues = new List<ValidationIssue>();
            string approvedBy = decision.ApprovedBy?.Trim() ?? string.Empty;
            string reason = decision.Reason?.Trim() ?? string.Empty;
            if (approvedBy.Length == 0)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ApprovalMissing, "Enter the reviewer name (ApprovedBy)."));
            }
            else if (approvedBy.Length > MaxApprovedByLength)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ApprovalMissing, $"The reviewer name must be at most {MaxApprovedByLength} characters."));
            }

            if (decision.GroupId != group.GroupId)
            {
                issues.Add(new ValidationIssue(ValidationCodes.StaleEvidence, "The decision belongs to a different group."));
            }

            if (string.IsNullOrEmpty(decision.ExpectedEvidenceHash)
                || !string.Equals(decision.ExpectedEvidenceHash, environment.CurrentEvidenceHash, StringComparison.Ordinal))
            {
                issues.Add(new ValidationIssue(ValidationCodes.StaleEvidence, "The group's evidence changed since it was reviewed. Reload the group and review it again."));
            }

            if (!decision.Action.HasValue || !Enum.IsDefined(decision.Action.Value))
            {
                issues.Add(new ValidationIssue(ValidationCodes.UnsupportedAction, "Choose ReconcileIdentifiers, RetireIdentifier, or DeleteGroup."));
                AddMigrationIssues(environment, issues);
                return issues;
            }

            ReconciliationAction action = decision.Action.Value;
            if (reason.Length == 0)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ReasonOrOwner, "Every action needs a reason."));
            }
            else if (reason.Length > MaxReasonLength)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ReasonOrOwner, $"The reason must be at most {MaxReasonLength} characters."));
            }

            if (action == ReconciliationAction.ReconcileIdentifiers && !decision.CanonicalPaymentId.HasValue)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ReasonOrOwner, "ReconcileIdentifiers requires an owner payment."));
            }

            if (action != ReconciliationAction.ReconcileIdentifiers && decision.CanonicalPaymentId.HasValue)
            {
                issues.Add(new ValidationIssue(ValidationCodes.ReasonOrOwner, $"{action} requires no owner payment."));
            }

            if (action == ReconciliationAction.ReconcileIdentifiers
                && decision.CanonicalPaymentId.HasValue
                && group.FindMember(decision.CanonicalPaymentId) == null)
            {
                issues.Add(new ValidationIssue(ValidationCodes.OwnerNotMember, $"Payment {decision.CanonicalPaymentId} is not a member of group {group.GroupId}."));
            }

            if (action == ReconciliationAction.RetireIdentifier && group.ConfirmedOwnerCount > 0)
            {
                issues.Add(new ValidationIssue(ValidationCodes.RetireConfirmed, "RetireIdentifier is not allowed when a member is confirmed by a donation. Choose an owner with ReconcileIdentifiers."));
            }

            if (action == ReconciliationAction.DeleteGroup)
            {
                AddDeletionIssues(group, environment, issues);
            }

            AddMigrationIssues(environment, issues);
            AddAuditIssues(group, action, decision.CanonicalPaymentId, environment, issues);
            return issues;
        }

        /// <summary>
        /// Lists the members whose provider ID the action clears or whose rows it deletes.
        /// </summary>
        /// <param name="group">Evaluated group.</param>
        /// <param name="action">Action.</param>
        /// <param name="canonicalPaymentId">Owner for reconciliation.</param>
        /// <returns>Affected payment IDs.</returns>
        public static IReadOnlyList<int> AffectedPaymentIds(GroupEvaluation group, ReconciliationAction action, int? canonicalPaymentId)
        {
            ArgumentNullException.ThrowIfNull(group);
            return group.Members
                .Where(member => action != ReconciliationAction.ReconcileIdentifiers || member.PaymentId != canonicalPaymentId)
                .Select(member => member.PaymentId)
                .ToList();
        }

        private static void AddDeletionIssues(GroupEvaluation group, ExecutionEnvironment environment, List<ValidationIssue> issues)
        {
            if (!group.HasSafeDeletionEvidence)
            {
                IEnumerable<int> unsafeMembers = group.Members.Where(member => !member.DeletionBasis.HasValue).Select(member => member.PaymentId);
                issues.Add(new ValidationIssue(
                    ValidationCodes.UnsafeDeletion,
                    "DeleteGroup requires every member to be an unpaid, uncompleted, unconfirmed attempt that is superseded by a different successful payment or abandoned without a donation link or successful status. Members without a basis: " + string.Join(", ", unsafeMembers) + "."));
            }

            if (!environment.HasViewDefinitionPermission)
            {
                issues.Add(new ValidationIssue(ValidationCodes.DeletionDependencies, "DeleteGroup requires VIEW DEFINITION on the database to check foreign keys."));
            }

            if (environment.UnsupportedForeignKeys.Count > 0)
            {
                issues.Add(new ValidationIssue(ValidationCodes.DeletionDependencies, "DeleteGroup requires single-column foreign keys to Payments.Id. Unsupported: " + string.Join(", ", environment.UnsupportedForeignKeys) + "."));
            }

            if (environment.EnabledPaymentTriggerCount > 0)
            {
                issues.Add(new ValidationIssue(ValidationCodes.DeletionDependencies, "DeleteGroup is not allowed while Payments has enabled triggers."));
            }

            HashSet<int> memberIds = group.Members.Select(member => member.PaymentId).ToHashSet();
            List<PaymentDependency> dependencies = environment.Dependencies
                .Where(dependency => memberIds.Contains(dependency.PaymentId) && dependency.ReferenceCount > 0)
                .ToList();
            if (dependencies.Count > 0)
            {
                string details = string.Join("; ", dependencies.Select(dependency => string.Format(
                    CultureInfo.InvariantCulture,
                    "payment {0}: {1} row(s) in {2}.{3}",
                    dependency.PaymentId,
                    dependency.ReferenceCount,
                    dependency.ReferencingTable,
                    dependency.ReferencingColumn)));
                issues.Add(new ValidationIssue(ValidationCodes.DeletionDependencies, "DeleteGroup has dependent records; no cascade, SET NULL, or relationship rewrite is allowed: " + details + "."));
            }
        }

        private static void AddMigrationIssues(ExecutionEnvironment environment, List<ValidationIssue> issues)
        {
            if (environment.OverlengthProviderIds > 0 || environment.ConflictingInvoiceGroups > 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationCodes.MigrationRequirements,
                    string.Format(CultureInfo.InvariantCulture, "Provider-ID lengths ({0}) or invoice sequences ({1}) still violate migration requirements. Resolve them separately.", environment.OverlengthProviderIds, environment.ConflictingInvoiceGroups)));
            }
        }

        private static void AddAuditIssues(GroupEvaluation group, ReconciliationAction action, int? canonicalPaymentId, ExecutionEnvironment environment, List<ValidationIssue> issues)
        {
            IReadOnlySet<int> audited = action switch
            {
                ReconciliationAction.ReconcileIdentifiers => environment.ReconciliationAuditPaymentIds,
                ReconciliationAction.RetireIdentifier => environment.RetirementAuditPaymentIds,
                _ => environment.DeletionAuditPaymentIds,
            };
            List<int> conflicts = AffectedPaymentIds(group, action, canonicalPaymentId).Where(audited.Contains).ToList();
            if (conflicts.Count > 0)
            {
                issues.Add(new ValidationIssue(
                    ValidationCodes.AuditHistoryExists,
                    "Audit history already exists for payment(s) " + string.Join(", ", conflicts) + ". History cannot be overwritten; review the original provider IDs first."));
            }
        }
    }
}
