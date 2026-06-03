// -----------------------------------------------------------------------
// <copyright file="DonationReportCampaignRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Campaign-level row.
    /// </summary>
    public class DonationReportCampaignRow
    {
        /// <summary>
        /// Gets or sets campaign name stored on donations.
        /// </summary>
        public string CampaignName { get; set; }

        /// <summary>
        /// Gets or sets paid amount.
        /// </summary>
        public double PaidAmount { get; set; }

        /// <summary>
        /// Gets or sets paid count.
        /// </summary>
        public int PaidCount { get; set; }

        /// <summary>
        /// Gets or sets pending count.
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Gets or sets average paid ticket.
        /// </summary>
        public double AveragePaidAmount { get; set; }

        /// <summary>
        /// Gets or sets conversion rate (%).
        /// </summary>
        public double ConversionPercent { get; set; }
    }
}
