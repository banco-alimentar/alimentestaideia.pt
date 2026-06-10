// -----------------------------------------------------------------------
// <copyright file="DonationReportSubscriptionStatusRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Subscription count for a single status value.
    /// </summary>
    public class DonationReportSubscriptionStatusRow
    {
        /// <summary>
        /// Gets or sets the status key (enum name).
        /// </summary>
        public string StatusKey { get; set; }

        /// <summary>
        /// Gets or sets the localized status label.
        /// </summary>
        public string StatusLabel { get; set; }

        /// <summary>
        /// Gets or sets the number of subscriptions with this status.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the share of subscriptions with this status.
        /// </summary>
        public double SharePercent { get; set; }
    }
}
