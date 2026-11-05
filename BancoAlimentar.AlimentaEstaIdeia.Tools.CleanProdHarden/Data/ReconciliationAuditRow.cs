// -----------------------------------------------------------------------
// <copyright file="ReconciliationAuditRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Row in <c>dbo.EasyPayPaymentIdReconciliationAudit</c>.
    /// </summary>
    public class ReconciliationAuditRow
    {
        /// <summary>
        /// Gets or sets the payment whose provider ID was cleared.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the owner that kept the provider ID.
        /// </summary>
        public int CanonicalPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the cleared provider ID.
        /// </summary>
        public string OriginalEasyPayPaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the ID was cleared.
        /// </summary>
        public DateTime ReconciledAtUtc { get; set; }
    }
}
