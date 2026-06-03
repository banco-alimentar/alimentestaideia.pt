// -----------------------------------------------------------------------
// <copyright file="DonationReportDailyPoint.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;

    /// <summary>
    /// Daily aggregation point.
    /// </summary>
    public class DonationReportDailyPoint
    {
        /// <summary>
        /// Gets or sets the calendar date (local campaign day).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets paid amount on this date.
        /// </summary>
        public double PaidAmount { get; set; }

        /// <summary>
        /// Gets or sets paid donation count.
        /// </summary>
        public int PaidCount { get; set; }
    }
}
