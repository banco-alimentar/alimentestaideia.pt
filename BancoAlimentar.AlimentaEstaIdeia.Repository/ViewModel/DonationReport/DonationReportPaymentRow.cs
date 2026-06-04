// -----------------------------------------------------------------------
// <copyright file="DonationReportPaymentRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Payment method aggregation row.
    /// </summary>
    public class DonationReportPaymentRow
    {
        /// <summary>
        /// Gets or sets payment type key.
        /// </summary>
        public string PaymentTypeKey { get; set; }

        /// <summary>
        /// Gets or sets human-readable label.
        /// </summary>
        public string PaymentTypeLabel { get; set; }

        /// <summary>
        /// Gets or sets paid amount.
        /// </summary>
        public double PaidAmount { get; set; }

        /// <summary>
        /// Gets or sets paid count.
        /// </summary>
        public int PaidCount { get; set; }

        /// <summary>
        /// Gets or sets average ticket.
        /// </summary>
        public double AveragePaidAmount { get; set; }

        /// <summary>
        /// Gets or sets share of paid volume (%).
        /// </summary>
        public double SharePercent { get; set; }

        /// <summary>
        /// Gets or sets the largest single paid donation for this method (EUR).
        /// </summary>
        public double MaxPaidAmount { get; set; }
    }
}
