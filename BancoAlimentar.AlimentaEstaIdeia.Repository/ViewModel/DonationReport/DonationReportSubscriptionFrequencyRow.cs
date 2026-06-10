// -----------------------------------------------------------------------
// <copyright file="DonationReportSubscriptionFrequencyRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Subscription metrics grouped by billing frequency.
    /// </summary>
    public class DonationReportSubscriptionFrequencyRow
    {
        /// <summary>
        /// Gets or sets the frequency display label.
        /// </summary>
        public string FrequencyLabel { get; set; }

        /// <summary>
        /// Gets or sets the number of subscriptions with this frequency.
        /// </summary>
        public int SubscriptionCount { get; set; }

        /// <summary>
        /// Gets or sets the total paid amount from linked donations in scope.
        /// </summary>
        public double TotalPaidAmount { get; set; }

        /// <summary>
        /// Gets or sets the share of subscriptions with this frequency.
        /// </summary>
        public double SubscriptionSharePercent { get; set; }

        /// <summary>
        /// Gets or sets the average paid donation amount for this frequency.
        /// </summary>
        public double AverageDonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the expected amount from active subscriptions until the forecast period end.
        /// </summary>
        public double ExpectedUpcomingAmount { get; set; }
    }
}
