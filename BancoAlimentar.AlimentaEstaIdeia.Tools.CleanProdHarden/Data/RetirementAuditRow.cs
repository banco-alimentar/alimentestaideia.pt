// -----------------------------------------------------------------------
// <copyright file="RetirementAuditRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Row in <c>dbo.EasyPayPaymentIdRetirementAudit</c>.
    /// </summary>
    public class RetirementAuditRow
    {
        /// <summary>
        /// Gets or sets the payment whose provider ID was retired.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the duplicate group.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Gets or sets the retired provider ID.
        /// </summary>
        public string OriginalEasyPayPaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plan hash.
        /// </summary>
        public string PlanEvidenceHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the ID was retired.
        /// </summary>
        public DateTime RetiredAtUtc { get; set; }
    }
}
