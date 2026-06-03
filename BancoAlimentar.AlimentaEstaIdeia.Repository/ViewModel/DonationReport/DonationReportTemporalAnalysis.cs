// -----------------------------------------------------------------------
// <copyright file="DonationReportTemporalAnalysis.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Time-of-day and day-of-week donation patterns.
    /// </summary>
    public class DonationReportTemporalAnalysis
    {
        /// <summary>
        /// Gets or sets the peak hour (0–23).
        /// </summary>
        public int PeakHour { get; set; }

        /// <summary>
        /// Gets or sets the peak hour label.
        /// </summary>
        public string PeakHourLabel { get; set; }

        /// <summary>
        /// Gets or sets donation count at peak hour.
        /// </summary>
        public int PeakHourCount { get; set; }

        /// <summary>
        /// Gets or sets the peak day of week.
        /// </summary>
        public DayOfWeek PeakDayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the peak day label.
        /// </summary>
        public string PeakDayLabel { get; set; }

        /// <summary>
        /// Gets or sets donation count on peak day.
        /// </summary>
        public int PeakDayCount { get; set; }

        /// <summary>
        /// Gets or sets hourly distribution (24 buckets).
        /// </summary>
        public IList<DonationReportHourlyPoint> HourlyDistribution { get; set; } = new List<DonationReportHourlyPoint>();

        /// <summary>
        /// Gets or sets weekday distribution (Monday–Sunday).
        /// </summary>
        public IList<DonationReportWeekdayPoint> WeekdayDistribution { get; set; } = new List<DonationReportWeekdayPoint>();
    }
}
