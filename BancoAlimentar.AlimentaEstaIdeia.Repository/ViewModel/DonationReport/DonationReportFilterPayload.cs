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
        /// Sentinel key for donations without a <see cref="Model.Donation.CampaignId"/>.
        /// </summary>
        public const string NoCampaignKey = "__none__";

        /// <summary>
        /// Sentinel key for no período oficial filter (all donations).
        /// </summary>
        public const string PeriodoOficialAllKey = "__periodo_all__";

        /// <summary>
        /// Filter key for donations within the official campaign period.
        /// </summary>
        public const string PeriodoOficialTrueKey = "true";

        /// <summary>
        /// Filter key for donations outside the official campaign period.
        /// </summary>
        public const string PeriodoOficialFalseKey = "false";

        /// <summary>
        /// Gets or sets aggregated metrics for all campaigns combined.
        /// </summary>
        public DonationReportCampaignDetail All { get; set; }

        /// <summary>
        /// Gets or sets aggregated metrics for donations with <see cref="Model.Donation.PeriodoOficial"/> set.
        /// </summary>
        public DonationReportCampaignDetail AllPeriodoOficial { get; set; }

        /// <summary>
        /// Gets or sets aggregated metrics for donations outside the official period.
        /// </summary>
        public DonationReportCampaignDetail AllForaPeriodoOficial { get; set; }

        /// <summary>
        /// Gets or sets filter dropdown options.
        /// </summary>
        public IList<DonationReportCampaignFilterOption> Options { get; set; } = new List<DonationReportCampaignFilterOption>();

        /// <summary>
        /// Gets or sets metrics broken down by campaign.
        /// </summary>
        public IList<DonationReportCampaignDetail> Campaigns { get; set; } = new List<DonationReportCampaignDetail>();

        /// <summary>
        /// Gets or sets per-campaign metrics limited to official-period donations.
        /// </summary>
        public IList<DonationReportCampaignDetail> CampaignsPeriodoOficial { get; set; } = new List<DonationReportCampaignDetail>();

        /// <summary>
        /// Gets or sets per-campaign metrics limited to donations outside the official period.
        /// </summary>
        public IList<DonationReportCampaignDetail> CampaignsForaPeriodoOficial { get; set; } = new List<DonationReportCampaignDetail>();

        /// <summary>
        /// Gets or sets período oficial filter dropdown options.
        /// </summary>
        public IList<DonationReportCampaignFilterOption> PeriodoOficialOptions { get; set; } =
            new List<DonationReportCampaignFilterOption>();

        /// <summary>
        /// Gets or sets cross-campaign evolution data.
        /// </summary>
        public DonationReportCampaignComparison Comparison { get; set; }

        /// <summary>
        /// Gets or sets cross-campaign evolution data for official-period donations only.
        /// </summary>
        public DonationReportCampaignComparison ComparisonPeriodoOficial { get; set; }

        /// <summary>
        /// Gets or sets cross-campaign evolution data for donations outside the official period.
        /// </summary>
        public DonationReportCampaignComparison ComparisonForaPeriodoOficial { get; set; }
    }
}
