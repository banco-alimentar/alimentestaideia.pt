// -----------------------------------------------------------------------
// <copyright file="DonationReportCampaignDetail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregated metrics for a single campaign (used by client-side filters).
    /// </summary>
    public class DonationReportCampaignDetail
    {
        /// <summary>
        /// Gets or sets the campaign key (stringified <see cref="Model.Donation.CampaignId"/> or <see cref="DonationReportFilterPayload.NoCampaignKey"/>).
        /// </summary>
        public string CampaignKey { get; set; }

        /// <summary>
        /// Gets or sets the campaign display name.
        /// </summary>
        public string CampaignName { get; set; }

        /// <summary>
        /// Gets or sets the earliest paid donation date in this campaign.
        /// </summary>
        public DateTime? PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the latest donation date in this campaign.
        /// </summary>
        public DateTime? PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets headline KPIs for the campaign.
        /// </summary>
        public DonationReportSummary Summary { get; set; }

        /// <summary>
        /// Gets or sets per food bank metrics.
        /// </summary>
        public IList<DonationReportFoodBankRow> FoodBanks { get; set; } = new List<DonationReportFoodBankRow>();

        /// <summary>
        /// Gets or sets per product metrics.
        /// </summary>
        public IList<DonationReportProductRow> Products { get; set; } = new List<DonationReportProductRow>();

        /// <summary>
        /// Gets or sets per payment method metrics.
        /// </summary>
        public IList<DonationReportPaymentRow> Payments { get; set; } = new List<DonationReportPaymentRow>();

        /// <summary>
        /// Gets or sets daily paid trend within the campaign.
        /// </summary>
        public IList<DonationReportDailyPoint> DailyTrend { get; set; } = new List<DonationReportDailyPoint>();

        /// <summary>
        /// Gets or sets payment status funnel for the campaign.
        /// </summary>
        public IList<DonationReportStatusRow> PaymentStatuses { get; set; } = new List<DonationReportStatusRow>();

        /// <summary>
        /// Gets or sets time-of-day and weekday patterns for the campaign.
        /// </summary>
        public DonationReportTemporalAnalysis TemporalAnalysis { get; set; }

        /// <summary>
        /// Gets or sets pending donation count (for campaigns table).
        /// </summary>
        public int PendingCount { get; set; }

        /// <summary>
        /// Gets or sets payment conversion percent (for campaigns table).
        /// </summary>
        public double ConversionPercent { get; set; }
    }
}
