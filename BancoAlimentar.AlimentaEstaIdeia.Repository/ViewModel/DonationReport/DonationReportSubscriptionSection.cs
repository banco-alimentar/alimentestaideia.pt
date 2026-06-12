// -----------------------------------------------------------------------
// <copyright file="DonationReportSubscriptionSection.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Subscription analytics for static donation reports.
    /// </summary>
    public class DonationReportSubscriptionSection
    {
        /// <summary>
        /// Gets or sets the total amount from paid subscription-linked donations.
        /// </summary>
        public double TotalPaidAmount { get; set; }

        /// <summary>
        /// Gets or sets the number of paid subscription-linked donations.
        /// </summary>
        public int PaidDonationCount { get; set; }

        /// <summary>
        /// Gets or sets the number of subscriptions in scope.
        /// </summary>
        public int SubscriptionCount { get; set; }

        /// <summary>
        /// Gets or sets subscription counts grouped by status.
        /// </summary>
        public IList<DonationReportSubscriptionStatusRow> StatusBreakdown { get; set; } =
            new List<DonationReportSubscriptionStatusRow>();

        /// <summary>
        /// Gets or sets subscription metrics grouped by billing frequency.
        /// </summary>
        public IList<DonationReportSubscriptionFrequencyRow> FrequencyBreakdown { get; set; } =
            new List<DonationReportSubscriptionFrequencyRow>();

        /// <summary>
        /// Gets or sets per-subscription metrics.
        /// </summary>
        public IList<DonationReportSubscriptionRow> Subscriptions { get; set; } =
            new List<DonationReportSubscriptionRow>();

        /// <summary>
        /// Gets or sets when the upcoming revenue forecast starts.
        /// </summary>
        public DateTime? ForecastPeriodStart { get; set; }

        /// <summary>
        /// Gets or sets when the upcoming revenue forecast ends (active campaign report end).
        /// </summary>
        public DateTime? ForecastPeriodEnd { get; set; }
    }
}
