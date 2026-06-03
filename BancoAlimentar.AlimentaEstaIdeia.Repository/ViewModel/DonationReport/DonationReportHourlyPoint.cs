// -----------------------------------------------------------------------
// <copyright file="DonationReportHourlyPoint.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Hour-of-day aggregation for temporal analysis.
    /// </summary>
    public class DonationReportHourlyPoint
    {
        /// <summary>
        /// Gets or sets the hour (0–23).
        /// </summary>
        public int Hour { get; set; }

        /// <summary>
        /// Gets or sets a display label for the hour bucket.
        /// </summary>
        public string HourLabel { get; set; }

        /// <summary>
        /// Gets or sets paid donation count in this hour.
        /// </summary>
        public int PaidCount { get; set; }

        /// <summary>
        /// Gets or sets paid amount in this hour (EUR).
        /// </summary>
        public double PaidAmount { get; set; }
    }
}
