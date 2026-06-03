// -----------------------------------------------------------------------
// <copyright file="DonationReportWeekdayPoint.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;

    /// <summary>
    /// Day-of-week aggregation for temporal analysis.
    /// </summary>
    public class DonationReportWeekdayPoint
    {
        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets a localized day name.
        /// </summary>
        public string DayLabel { get; set; }

        /// <summary>
        /// Gets or sets paid donation count on this weekday.
        /// </summary>
        public int PaidCount { get; set; }

        /// <summary>
        /// Gets or sets paid amount on this weekday (EUR).
        /// </summary>
        public double PaidAmount { get; set; }
    }
}
