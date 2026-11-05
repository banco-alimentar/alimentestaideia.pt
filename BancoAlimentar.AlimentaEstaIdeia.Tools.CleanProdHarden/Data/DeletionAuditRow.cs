// -----------------------------------------------------------------------
// <copyright file="DeletionAuditRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Row in <c>dbo.EasyPayPaymentDeletionAudit</c>.
    /// </summary>
    public class DeletionAuditRow
    {
        /// <summary>
        /// Gets or sets the deleted payment.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the duplicate group.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Gets or sets the payment that paid the donation instead.
        /// </summary>
        public int? ReplacementPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the deleted row's provider ID.
        /// </summary>
        public string OriginalEasyPayPaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full deleted row as JSON.
        /// </summary>
        public string DeletedPaymentJson { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plan hash.
        /// </summary>
        public string PlanEvidenceHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the row was deleted.
        /// </summary>
        public DateTime DeletedAtUtc { get; set; }
    }
}
