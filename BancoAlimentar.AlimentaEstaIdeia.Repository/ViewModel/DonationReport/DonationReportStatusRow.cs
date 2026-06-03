// -----------------------------------------------------------------------
// <copyright file="DonationReportStatusRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Payment status funnel row.
    /// </summary>
    public class DonationReportStatusRow
    {
        /// <summary>
        /// Gets or sets status label.
        /// </summary>
        public string StatusLabel { get; set; }

        /// <summary>
        /// Gets or sets donation count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets share of all donations in period (%).
        /// </summary>
        public double SharePercent { get; set; }
    }
}
