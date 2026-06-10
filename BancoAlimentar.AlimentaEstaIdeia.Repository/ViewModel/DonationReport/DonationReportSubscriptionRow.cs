// -----------------------------------------------------------------------
// <copyright file="DonationReportSubscriptionRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;

    /// <summary>
    /// Per-subscription metrics for static reports.
    /// </summary>
    public class DonationReportSubscriptionRow
    {
        /// <summary>
        /// Gets or sets the internal subscription identifier for admin detail links.
        /// </summary>
        public int SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the subscription public identifier.
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// Gets or sets the subscription status key (enum name).
        /// </summary>
        public string StatusKey { get; set; }

        /// <summary>
        /// Gets or sets the subscription status label.
        /// </summary>
        public string StatusLabel { get; set; }

        /// <summary>
        /// Gets or sets the normalized frequency key used for report filters.
        /// </summary>
        public string FrequencyKey { get; set; }

        /// <summary>
        /// Gets or sets the billing frequency label.
        /// </summary>
        public string Frequency { get; set; }

        /// <summary>
        /// Gets or sets when the subscription was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the number of paid linked donations in scope.
        /// </summary>
        public int PaidDonationCount { get; set; }

        /// <summary>
        /// Gets or sets the total paid amount from linked donations in scope.
        /// </summary>
        public double TotalPaidAmount { get; set; }
    }
}
