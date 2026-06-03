// -----------------------------------------------------------------------
// <copyright file="DonationReportHtmlGeneratorTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport;
    using Xunit;

    /// <summary>
    /// Tests for static HTML report generation.
    /// </summary>
    public class DonationReportHtmlGeneratorTests
    {
        /// <summary>
        /// Ensures all expected pages are produced with chart markup.
        /// </summary>
        [Fact]
        public void GenerateAllPages_IncludesNavigationAndCharts()
        {
            DonationReportSnapshot snapshot = BuildSampleSnapshot();
            IReadOnlyDictionary<string, string> pages = DonationReportHtmlGenerator.GenerateAllPages(snapshot, "Alimente esta ideia — Relatório");

            Assert.Equal(11, pages.Count);
            Assert.Contains("index.html", pages.Keys);
            Assert.Contains("campaigns.html", pages.Keys);
            Assert.Contains("campaign-evolution.html", pages.Keys);
            Assert.Contains("food-banks.html", pages.Keys);
            Assert.Contains("products.html", pages.Keys);
            Assert.Contains("payments.html", pages.Keys);
            Assert.Contains("timing.html", pages.Keys);
            Assert.Contains("cross-analysis.html", pages.Keys);
            Assert.Contains("styles.css", pages.Keys);
            Assert.Contains("report-data.json", pages.Keys);
            Assert.Contains("report-filters.js", pages.Keys);

            Assert.Contains("Painel executivo", pages["index.html"]);
            Assert.Contains("chart.js", pages["index.html"], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("site-header", pages["index.html"]);
            Assert.Contains("styles.css", pages["index.html"]);
            Assert.Contains("Total angariado", pages["index.html"]);
            Assert.Contains("href=\"payments.html\"", pages["index.html"]);
            Assert.Contains("campaignFilter", pages["index.html"]);
            Assert.Contains("reportFilterData", pages["index.html"]);
            Assert.Contains("Evolução entre campanhas", pages["campaign-evolution.html"]);
            Assert.Contains("donationStatsChart", pages["campaign-evolution.html"]);
            Assert.Contains("Valor da doação por campanha", pages["campaign-evolution.html"]);
            Assert.Contains("Máximo (€)", pages["payments.html"]);
            Assert.Contains("payCampaignAvgChart", pages["payments.html"]);
            Assert.Contains("payCampaignMaxChart", pages["payments.html"]);
            Assert.Contains("Média por campanha", pages["payments.html"]);
            Assert.Contains("Análise temporal", pages["timing.html"]);
            Assert.Contains("hourCountChart", pages["timing.html"]);
            Assert.Contains("__all__", pages["report-data.json"]);
            Assert.Contains("getCampaignKey", pages["report-filters.js"]);
        }

        private static DonationReportSnapshot BuildSampleSnapshot()
        {
            return new DonationReportSnapshot
            {
                GeneratedAtUtc = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc),
                TenantDisplayName = "Portugal",
                PeriodStart = new DateTime(2026, 5, 1),
                PeriodEnd = new DateTime(2026, 6, 2),
                CampaignLabel = "Todas as campanhas",
                Summary = new DonationReportSummary
                {
                    TotalPaidAmount = 125000,
                    PaidDonationCount = 4200,
                    PendingDonationCount = 180,
                    FailedDonationCount = 12,
                    AveragePaidAmount = 29.76,
                    MaxSingleDonation = 500,
                    TotalProductUnits = 98000,
                    TotalProductValue = 110000,
                    CashDonationSharePercent = 8.5,
                    PaymentConversionPercent = 95.2,
                    ActiveFoodBankCount = 21,
                },
                DailyTrend = new List<DonationReportDailyPoint>
                {
                    new DonationReportDailyPoint { Date = new DateTime(2026, 6, 1), PaidAmount = 4000, PaidCount = 120 },
                    new DonationReportDailyPoint { Date = new DateTime(2026, 6, 2), PaidAmount = 5200, PaidCount = 150 },
                },
                Campaigns = new List<DonationReportCampaignRow>
                {
                    new DonationReportCampaignRow { CampaignName = "2026", PaidAmount = 125000, PaidCount = 4200, PendingCount = 180, AveragePaidAmount = 29.76, ConversionPercent = 95.2 },
                },
                FoodBanks = new List<DonationReportFoodBankRow>
                {
                    new DonationReportFoodBankRow { FoodBankId = 1, FoodBankName = "Lisboa", PaidAmount = 50000, PaidCount = 1500, ProductUnits = 40000, SharePercent = 40 },
                },
                Products = new List<DonationReportProductRow>
                {
                    new DonationReportProductRow { ProductName = "Arroz", UnitOfMeasure = "kg", Quantity = 20000, Value = 30000, SharePercent = 20 },
                },
                Payments = new List<DonationReportPaymentRow>
                {
                    new DonationReportPaymentRow { PaymentTypeKey = "MBWay", PaymentTypeLabel = "MBWay", PaidAmount = 70000, PaidCount = 2500, AveragePaidAmount = 28, MaxPaidAmount = 500, SharePercent = 56 },
                },
                TemporalAnalysis = new DonationReportTemporalAnalysis
                {
                    PeakHour = 20,
                    PeakHourLabel = "20h",
                    PeakHourCount = 420,
                    PeakDayOfWeek = DayOfWeek.Saturday,
                    PeakDayLabel = "sábado",
                    PeakDayCount = 900,
                    HourlyDistribution = new List<DonationReportHourlyPoint>
                    {
                        new DonationReportHourlyPoint { Hour = 20, HourLabel = "20h", PaidCount = 420, PaidAmount = 12000 },
                    },
                    WeekdayDistribution = new List<DonationReportWeekdayPoint>
                    {
                        new DonationReportWeekdayPoint { DayOfWeek = DayOfWeek.Saturday, DayLabel = "sábado", PaidCount = 900, PaidAmount = 28000 },
                    },
                },
                PaymentStatuses = new List<DonationReportStatusRow>
                {
                    new DonationReportStatusRow { StatusLabel = "Pago", Count = 4200, SharePercent = 95 },
                },
                FoodBankByProduct = new List<DonationReportCrossRow>
                {
                    new DonationReportCrossRow { DimensionA = "Lisboa", DimensionB = "Arroz", Amount = 10000, Count = 5000 },
                },
                CampaignByPayment = new List<DonationReportCrossRow>
                {
                    new DonationReportCrossRow { DimensionA = "2026", DimensionB = "MBWay", Amount = 70000, Count = 2500 },
                },
                FoodBankByPayment = new List<DonationReportCrossRow>
                {
                    new DonationReportCrossRow { DimensionA = "Lisboa", DimensionB = "MBWay", Amount = 30000, Count = 1000 },
                },
                Filters = new DonationReportFilterPayload
                {
                    All = new DonationReportCampaignDetail
                    {
                        CampaignKey = DonationReportFilterPayload.AllCampaignsKey,
                        CampaignName = "Todas as campanhas",
                        PeriodStart = new DateTime(2026, 5, 1),
                        PeriodEnd = new DateTime(2026, 6, 2),
                        Summary = new DonationReportSummary
                        {
                            TotalPaidAmount = 125000,
                            PaidDonationCount = 4200,
                            PendingDonationCount = 180,
                            FailedDonationCount = 12,
                            AveragePaidAmount = 29.76,
                            MaxSingleDonation = 500,
                            TotalProductUnits = 98000,
                            TotalProductValue = 110000,
                            CashDonationSharePercent = 8.5,
                            PaymentConversionPercent = 95.2,
                            ActiveFoodBankCount = 21,
                        },
                        DailyTrend = new List<DonationReportDailyPoint>
                        {
                            new DonationReportDailyPoint { Date = new DateTime(2026, 6, 1), PaidAmount = 4000, PaidCount = 120 },
                        },
                        PaymentStatuses = new List<DonationReportStatusRow>
                        {
                            new DonationReportStatusRow { StatusLabel = "Pago", Count = 4200, SharePercent = 95 },
                        },
                        Payments = new List<DonationReportPaymentRow>
                        {
                            new DonationReportPaymentRow { PaymentTypeKey = "MBWay", PaymentTypeLabel = "MBWay", PaidAmount = 70000, PaidCount = 2500, AveragePaidAmount = 28, MaxPaidAmount = 500, SharePercent = 56 },
                        },
                        TemporalAnalysis = new DonationReportTemporalAnalysis
                        {
                            PeakHourLabel = "20h",
                            PeakHourCount = 420,
                            PeakDayLabel = "sábado",
                            PeakDayCount = 900,
                            HourlyDistribution = new List<DonationReportHourlyPoint>
                            {
                                new DonationReportHourlyPoint { Hour = 20, HourLabel = "20h", PaidCount = 420, PaidAmount = 12000 },
                            },
                            WeekdayDistribution = new List<DonationReportWeekdayPoint>
                            {
                                new DonationReportWeekdayPoint { DayOfWeek = DayOfWeek.Saturday, DayLabel = "sábado", PaidCount = 900, PaidAmount = 28000 },
                            },
                        },
                        FoodBanks = new List<DonationReportFoodBankRow>
                        {
                            new DonationReportFoodBankRow { FoodBankName = "Lisboa", PaidAmount = 50000, PaidCount = 1500, ProductUnits = 40000, SharePercent = 40 },
                        },
                        Products = new List<DonationReportProductRow>
                        {
                            new DonationReportProductRow { ProductName = "Arroz", UnitOfMeasure = "kg", Quantity = 20000, Value = 30000, SharePercent = 20 },
                        },
                        PendingCount = 180,
                        ConversionPercent = 95.2,
                    },
                    Options = new List<DonationReportCampaignFilterOption>
                    {
                        new DonationReportCampaignFilterOption
                        {
                            Key = DonationReportFilterPayload.AllCampaignsKey,
                            Label = "Todas as campanhas",
                        },
                        new DonationReportCampaignFilterOption { Key = "2026", Label = "2026" },
                    },
                    Campaigns = new List<DonationReportCampaignDetail>
                    {
                        new DonationReportCampaignDetail
                        {
                            CampaignKey = "2026",
                            CampaignName = "2026",
                            PeriodStart = new DateTime(2026, 5, 1),
                            PeriodEnd = new DateTime(2026, 6, 2),
                            Summary = new DonationReportSummary
                            {
                                TotalPaidAmount = 125000,
                                PaidDonationCount = 4200,
                                ActiveFoodBankCount = 21,
                            },
                            FoodBanks = new List<DonationReportFoodBankRow>
                            {
                                new DonationReportFoodBankRow { FoodBankName = "Lisboa", PaidAmount = 50000, PaidCount = 1500, ProductUnits = 40000, SharePercent = 40 },
                            },
                            Products = new List<DonationReportProductRow>
                            {
                                new DonationReportProductRow { ProductName = "Arroz", UnitOfMeasure = "kg", Quantity = 20000, Value = 30000, SharePercent = 20 },
                            },
                        },
                    },
                    Comparison = new DonationReportCampaignComparison
                    {
                        CampaignLabels = new List<string> { "2025", "2026" },
                        CampaignTotals = new List<double> { 98000, 125000 },
                        CampaignAverageDonations = new List<double> { 25.5, 29.76 },
                        CampaignMedianDonations = new List<double> { 20, 24 },
                        CampaignMaxDonations = new List<double> { 400, 500 },
                        CampaignMinDonations = new List<double> { 2.5, 2.5 },
                        FoodBankAmountSeries = new List<DonationReportSeriesRow>
                        {
                            new DonationReportSeriesRow { Label = "Lisboa", Values = new[] { 38000.0, 50000.0 } },
                        },
                        ProductUnitSeries = new List<DonationReportSeriesRow>
                        {
                            new DonationReportSeriesRow { Label = "Arroz", Values = new[] { 15000.0, 20000.0 } },
                        },
                    },
                },
            };
        }
    }
}
