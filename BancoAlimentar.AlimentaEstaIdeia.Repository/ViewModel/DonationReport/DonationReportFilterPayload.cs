// -----------------------------------------------------------------------
// <copyright file="DonationReportFilterPayload.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System.Collections.Generic;

    /// <summary>
    /// JSON payload embedded in static reports for client-side campaign filtering.
    /// </summary>
    public class DonationReportFilterPayload
    {
        /// <summary>
        /// Sentinel key for the aggregated all-campaigns view (default).
        /// </summary>
        public const string AllCampaignsKey = "__all__";

        /// <summary>
        /// Gets or sets aggregated metrics for all campaigns combined.
        /// </summary>
        public DonationReportCampaignDetail All { get; set; }

        /// <summary>
        /// Gets or sets filter dropdown options.
        /// </summary>
        public IList<DonationReportCampaignFilterOption> Options { get; set; } = new List<DonationReportCampaignFilterOption>();

        /// <summary>
        /// Gets or sets metrics broken down by campaign.
        /// </summary>
        public IList<DonationReportCampaignDetail> Campaigns { get; set; } = new List<DonationReportCampaignDetail>();

        /// <summary>
        /// Gets or sets cross-campaign evolution data.
        /// </summary>
        public DonationReportCampaignComparison Comparison { get; set; }
    }
}
