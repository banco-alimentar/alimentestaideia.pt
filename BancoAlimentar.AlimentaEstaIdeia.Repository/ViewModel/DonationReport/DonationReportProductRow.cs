// -----------------------------------------------------------------------
// <copyright file="DonationReportProductRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Product catalogue aggregation row.
    /// </summary>
    public class DonationReportProductRow
    {
        /// <summary>
        /// Gets or sets product name.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Gets or sets unit of measure label.
        /// </summary>
        public string UnitOfMeasure { get; set; }

        /// <summary>
        /// Gets or sets units donated.
        /// </summary>
        public long Quantity { get; set; }

        /// <summary>
        /// Gets or sets catalogue value (quantity × unit price).
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets share of total units (%).
        /// </summary>
        public double SharePercent { get; set; }
    }
}
