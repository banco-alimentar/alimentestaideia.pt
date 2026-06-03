// -----------------------------------------------------------------------
// <copyright file="DonationReportSnapshot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregated donation analytics for static report generation.
    /// </summary>
    public class DonationReportSnapshot
    {
        /// <summary>
        /// Gets or sets when the snapshot was built (UTC).
        /// </summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the tenant display name.
        /// </summary>
        public string TenantDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the reporting period start (inclusive).
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets the reporting period end (inclusive).
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets the active campaign label.
        /// </summary>
        public string CampaignLabel { get; set; }

        /// <summary>
        /// Gets or sets headline KPIs.
        /// </summary>
        public DonationReportSummary Summary { get; set; }

        /// <summary>
        /// Gets or sets daily paid donation trend.
        /// </summary>
        public IList<DonationReportDailyPoint> DailyTrend { get; set; } = new List<DonationReportDailyPoint>();

        /// <summary>
        /// Gets or sets per-campaign metrics (all campaigns, not only active).
        /// </summary>
        public IList<DonationReportCampaignRow> Campaigns { get; set; } = new List<DonationReportCampaignRow>();

        /// <summary>
        /// Gets or sets per food bank metrics for the active period.
        /// </summary>
        public IList<DonationReportFoodBankRow> FoodBanks { get; set; } = new List<DonationReportFoodBankRow>();

        /// <summary>
        /// Gets or sets per product metrics for the active period.
        /// </summary>
        public IList<DonationReportProductRow> Products { get; set; } = new List<DonationReportProductRow>();

        /// <summary>
        /// Gets or sets per payment type metrics for the active period.
        /// </summary>
        public IList<DonationReportPaymentRow> Payments { get; set; } = new List<DonationReportPaymentRow>();

        /// <summary>
        /// Gets or sets payment status funnel for the active period.
        /// </summary>
        public IList<DonationReportStatusRow> PaymentStatuses { get; set; } = new List<DonationReportStatusRow>();

        /// <summary>
        /// Gets or sets food bank × product cross-tab (top combinations).
        /// </summary>
        public IList<DonationReportCrossRow> FoodBankByProduct { get; set; } = new List<DonationReportCrossRow>();

        /// <summary>
        /// Gets or sets campaign × payment type cross-tab.
        /// </summary>
        public IList<DonationReportCrossRow> CampaignByPayment { get; set; } = new List<DonationReportCrossRow>();

        /// <summary>
        /// Gets or sets food bank × payment type cross-tab.
        /// </summary>
        public IList<DonationReportCrossRow> FoodBankByPayment { get; set; } = new List<DonationReportCrossRow>();
    }
}
