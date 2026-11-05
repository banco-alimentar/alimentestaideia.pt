// -----------------------------------------------------------------------
// <copyright file="DecisionEventRecord.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// Append-only history of saves, dry runs, and applies.
    /// </summary>
    public class DecisionEventRecord
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the target key.
        /// </summary>
        public string TargetKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group ID.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Gets or sets when the event happened.
        /// </summary>
        public DateTime AtUtc { get; set; }

        /// <summary>
        /// Gets or sets the event kind.
        /// </summary>
        public DecisionEventKind Kind { get; set; }

        /// <summary>
        /// Gets or sets whether a run succeeded.
        /// </summary>
        public bool? Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the reviewer.
        /// </summary>
        public string ApprovedBy { get; set; } = string.Empty;

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
        /// Gets or sets the evidence hash.
        /// </summary>
        public string EvidenceHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plan hash.
        /// </summary>
        public string? PlanEvidenceHash { get; set; }

        /// <summary>
        /// Gets or sets the run's issues.
        /// </summary>
        public string? Issues { get; set; }

        /// <summary>
        /// Gets or sets the run's step log.
        /// </summary>
        public string? Log { get; set; }
    }
}
