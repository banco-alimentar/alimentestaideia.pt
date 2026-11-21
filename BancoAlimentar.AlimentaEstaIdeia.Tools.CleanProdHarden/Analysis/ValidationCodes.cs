// -----------------------------------------------------------------------
// <copyright file="ValidationCodes.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Error numbers shared with <c>MergeDuplicateEasyPayPaymentIds.sql</c>.
    /// </summary>
    public static class ValidationCodes
    {
        /// <summary>
        /// Missing reason, or owner rules violated.
        /// </summary>
        public const int ReasonOrOwner = 51002;

        /// <summary>
        /// Audit history already exists.
        /// </summary>
        public const int AuditHistoryExists = 51003;

        /// <summary>
        /// Duplicates remain after the action.
        /// </summary>
        public const int DuplicatesRemain = 51004;

        /// <summary>
        /// Approval details are missing or invalid.
        /// </summary>
        public const int ApprovalMissing = 51006;

        /// <summary>
        /// The target or evidence changed.
        /// </summary>
        public const int StaleEvidence = 51007;

        /// <summary>
        /// The owner is not a group member.
        /// </summary>
        public const int OwnerNotMember = 51008;

        /// <summary>
        /// Other migration requirements are still violated.
        /// </summary>
        public const int MigrationRequirements = 51009;

        /// <summary>
        /// Missing or unsupported action.
        /// </summary>
        public const int UnsupportedAction = 51010;

        /// <summary>
        /// Delete without safe deletion evidence.
        /// </summary>
        public const int UnsafeDeletion = 51011;

        /// <summary>
        /// Delete blocked by permissions, foreign keys, triggers, or dependents.
        /// </summary>
        public const int DeletionDependencies = 51012;

        /// <summary>
        /// Retire while a member is confirmed.
        /// </summary>
        public const int RetireConfirmed = 51013;

        /// <summary>
        /// The typed database name does not match the connected database.
        /// </summary>
        public const int TargetNotConfirmed = 51100;

        /// <summary>
        /// The group no longer has duplicate provider IDs.
        /// </summary>
        public const int GroupNotFound = 51101;

        /// <summary>
        /// An update or delete changed a different number of rows than planned.
        /// </summary>
        public const int RowCountMismatch = 51102;

        /// <summary>
        /// Apply requires a passing dry run of the same decision.
        /// </summary>
        public const int DryRunRequired = 51103;

        /// <summary>
        /// Applying changes is disabled in configuration.
        /// </summary>
        public const int ApplyDisabled = 51104;

        /// <summary>
        /// SQL Server or the application raised an unexpected error.
        /// </summary>
        public const int UnexpectedError = 51199;
    }
}
