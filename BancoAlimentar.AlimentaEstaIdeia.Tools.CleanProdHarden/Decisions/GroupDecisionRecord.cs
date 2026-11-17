// -----------------------------------------------------------------------
// <copyright file="GroupDecisionRecord.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// Latest decision for one group on one target.
    /// </summary>
    public class GroupDecisionRecord
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the target key (<c>server|database</c>).
        /// </summary>
        public string TargetKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group ID.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Gets or sets the provider ID.
        /// </summary>
        public string EasyPayPaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the evidence hash the decision was made on.
        /// </summary>
        public string EvidenceHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        public ReconciliationAction? Action { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        public int? CanonicalPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the reason.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reviewer.
        /// </summary>
        public string ApprovedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets private notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public DecisionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the fingerprint of the last passing dry run.
        /// </summary>
        public string? DryRunFingerprint { get; set; }

        /// <summary>
        /// Gets or sets when the last dry run ran.
        /// </summary>
        public DateTime? DryRunAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the decision was applied.
        /// </summary>
        public DateTime? AppliedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the plan hash written by the apply.
        /// </summary>
        public string? PlanEvidenceHash { get; set; }

        /// <summary>
        /// Gets or sets the payments the last run affected.
        /// </summary>
        public string? AffectedPaymentIds { get; set; }

        /// <summary>
        /// Gets or sets the last run's issues.
        /// </summary>
        public string? LastIssues { get; set; }

        /// <summary>
        /// Gets or sets when the record was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the record last changed.
        /// </summary>
        public DateTime UpdatedAtUtc { get; set; }
    }
}
