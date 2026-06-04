// -----------------------------------------------------------------------
// <copyright file="DonationReportCrossRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Cross-dimensional aggregation row.
    /// </summary>
    public class DonationReportCrossRow
    {
        /// <summary>
        /// Gets or sets first dimension label (row).
        /// </summary>
        public string DimensionA { get; set; }

        /// <summary>
        /// Gets or sets second dimension label (column).
        /// </summary>
        public string DimensionB { get; set; }

        /// <summary>
        /// Gets or sets monetary amount when applicable.
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets count or quantity metric.
        /// </summary>
        public long Count { get; set; }
    }
}
