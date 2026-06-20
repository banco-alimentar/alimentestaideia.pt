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

            Assert.Equal(15, pages.Count);
            Assert.Contains("index.html", pages.Keys);
            Assert.Contains("campaigns.html", pages.Keys);
            Assert.Contains("donors.html", pages.Keys);
            Assert.Contains("campaign-evolution.html", pages.Keys);
            Assert.Contains("food-banks.html", pages.Keys);
            Assert.Contains("products.html", pages.Keys);
            Assert.Contains("payments.html", pages.Keys);
            Assert.Contains("subscriptions.html", pages.Keys);
            Assert.Contains("subscription-list.html", pages.Keys);
            Assert.Contains("user-logins.html", pages.Keys);
            Assert.Contains("timing.html", pages.Keys);
            Assert.Contains("cross-analysis.html", pages.Keys);
            Assert.Contains("styles.css", pages.Keys);
            Assert.Contains("report-data.json", pages.Keys);
            Assert.Contains("report-filters.js", pages.Keys);

            Assert.Contains("Painel executivo", pages["index.html"]);
            Assert.Contains("chart.js", pages["index.html"], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("site-header", pages["index.html"]);
            Assert.Contains("href=\"/\" class=\"brand-link\"", pages["index.html"]);
            Assert.Contains("Alimente esta ideia", pages["index.html"]);
            Assert.Contains("Relatório gerado em 2026-06-02 12:00 UTC", pages["index.html"]);
            Assert.Contains("Relatório gerado em 2026-06-02 12:00 UTC", pages["subscriptions.html"]);
            Assert.Contains("styles.css", pages["index.html"]);
            Assert.Contains("Total doado", pages["index.html"]);
            Assert.Contains("href=\"payments.html\"", pages["index.html"]);
            Assert.Contains("campaignFilter", pages["index.html"]);
            Assert.Contains("periodoOficialFilter", pages["index.html"]);
            Assert.Contains("reportFilterData", pages["index.html"]);
            Assert.Contains("Evolução entre campanhas", pages["campaign-evolution.html"]);
            string campaignEvolutionHtml = pages["campaign-evolution.html"];
            Assert.DoesNotContain("campaignFilter", campaignEvolutionHtml);
            Assert.Contains("periodoOficialFilter", campaignEvolutionHtml);
            Assert.Contains("campaignPeriodoTotalsChart", campaignEvolutionHtml);
            Assert.Contains("período oficial vs fora", campaignEvolutionHtml, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("campaignSubscriptionCountChart", campaignEvolutionHtml);
            Assert.Contains("Número de subscrições por campanha (total e por estado)", campaignEvolutionHtml);
            Assert.Contains("campaignDonationCountChart", campaignEvolutionHtml);
            Assert.Contains("Número de doações por campanha (total e por estado)", campaignEvolutionHtml);
            Assert.Contains("donationStatsChart", campaignEvolutionHtml);
            Assert.Contains("donationMaxChart", campaignEvolutionHtml);
            Assert.Contains("Valor da doação por campanha", campaignEvolutionHtml);
            Assert.Contains("Máximo por campanha", campaignEvolutionHtml);
            int statsChartIndex = campaignEvolutionHtml.IndexOf("donationStatsChart", StringComparison.Ordinal);
            int maxChartIndex = campaignEvolutionHtml.IndexOf("donationMaxChart", StringComparison.Ordinal);
            string donationStatsScript = campaignEvolutionHtml.Substring(statsChartIndex, maxChartIndex - statsChartIndex);
            Assert.DoesNotContain("label: 'Máximo'", donationStatsScript);
            Assert.Contains("Máximo (€)", pages["payments.html"]);
            Assert.Contains("payCampaignAvgChart", pages["payments.html"]);
            Assert.Contains("payCampaignMaxChart", pages["payments.html"]);
            Assert.Contains("Média por campanha", pages["payments.html"]);
            Assert.Contains("Análise temporal", pages["timing.html"]);
            Assert.Contains("hourCountChart", pages["timing.html"]);
            Assert.Contains("__all__", pages["report-data.json"]);
            Assert.Contains("getCampaignKey", pages["report-filters.js"]);
            Assert.Contains("Subscrições", pages["subscriptions.html"]);
            Assert.Contains("subscriptionStatusChart", pages["subscriptions.html"]);
            Assert.Contains("subscriptionFrequencyCountChart", pages["subscriptions.html"]);
            Assert.Contains("subscriptionFrequencyTableBody", pages["subscriptions.html"]);
            Assert.DoesNotContain("subscriptionListLink", pages["subscriptions.html"]);
            Assert.DoesNotContain("subscriptionTableBody", pages["subscriptions.html"]);
            Assert.Contains("subscriptionListTableBody", pages["subscription-list.html"]);
            Assert.Contains("subscriptionListPagination", pages["subscription-list.html"]);
            Assert.Contains("href=\"subscriptions.html\"", pages["index.html"]);
            Assert.Contains("updateSubscriptionsPage", pages["report-filters.js"]);
            Assert.Contains("renderSubscriptionFrequencyTable", pages["report-filters.js"]);
            Assert.Contains("Média doação (€)", pages["subscriptions.html"]);
            Assert.Contains("Previsto período (€)", pages["subscriptions.html"]);
            Assert.Contains("renderSubscriptionForecastPeriod", pages["report-filters.js"]);
            Assert.Contains("updateSubscriptionListPage", pages["report-filters.js"]);
            Assert.Contains("buildSubscriptionListUrl", pages["report-filters.js"]);
            Assert.Contains("Autenticação", pages["user-logins.html"]);
            Assert.Contains("userLoginCountChart", pages["user-logins.html"]);
            Assert.Contains("userLoginTableBody", pages["user-logins.html"]);
            Assert.Contains("href=\"user-logins.html\"", pages["index.html"]);
            Assert.Contains("updateUserLoginsPage", pages["report-filters.js"]);
            Assert.Contains("updateCampaignEvolutionPage", pages["report-filters.js"]);
            Assert.Contains("getPeriodoOficialKey", pages["report-filters.js"]);
            Assert.Contains("__periodo_all__", pages["report-data.json"]);
            Assert.Contains("Período oficial", pages["campaigns.html"]);
            Assert.Contains("stacked: true", pages["campaigns.html"]);
            Assert.Contains("getCampaignChartPeriodoData", pages["report-filters.js"]);
            Assert.Contains("Período oficial", pages["food-banks.html"]);
            Assert.Contains("stacked: true", pages["food-banks.html"]);
            Assert.Contains("updateFoodBankAmountChart", pages["report-filters.js"]);
            Assert.Contains("<th>Doadores</th>", pages["campaigns.html"]);
            Assert.Contains("<th>Mediana</th>", pages["campaigns.html"]);
            Assert.Contains("donorsChart", pages["donors.html"]);
            Assert.Contains("type: 'line'", pages["donors.html"]);
            Assert.Contains("donorsTableHead", pages["donors.html"]);
            Assert.Contains("donorsTableBody", pages["donors.html"]);
            Assert.Contains("donors-cross-tab", pages["donors.html"]);
            Assert.Contains("<th scope=\"col\">Total</th>", pages["donors.html"]);
            Assert.Contains("<th scope=\"row\">Total</th>", pages["donors.html"]);
            Assert.Contains("href=\"donors.html\"", pages["index.html"]);
            Assert.Contains("updateDonorsPage", pages["report-filters.js"]);
            Assert.Contains("renderDonorsCrossTab", pages["report-filters.js"]);
            Assert.Contains("updateDonorsChart", pages["report-filters.js"]);
            Assert.Contains("buildDonorsChartData", pages["report-filters.js"]);
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
                    new DonationReportCampaignRow
                    {
                        CampaignName = "2026",
                        PaidAmount = 125000,
                        PaidCount = 4200,
                        PendingCount = 180,
                        AveragePaidAmount = 29.76,
                        ConversionPercent = 95.2,
                        DistinctDonorCount = 3100,
                        MedianPaidAmount = 25,
                    },
                },
                Donors = new List<DonationReportDonorCampaignFoodBankRow>
                {
                    new DonationReportDonorCampaignFoodBankRow
                    {
                        CampaignKey = "1",
                        CampaignName = "2026",
                        FoodBankId = 1,
                        FoodBankName = "Lisboa",
                        DistinctDonorCount = 1500,
                    },
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
                Subscriptions = new DonationReportSubscriptionSection
                {
                    TotalPaidAmount = 15000,
                    PaidDonationCount = 420,
                    SubscriptionCount = 85,
                    ForecastPeriodStart = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc),
                    ForecastPeriodEnd = new DateTime(2026, 12, 31),
                    StatusBreakdown = new List<DonationReportSubscriptionStatusRow>
                    {
                        new DonationReportSubscriptionStatusRow
                        {
                            StatusKey = "Active",
                            StatusLabel = "Ativa",
                            Count = 60,
                            SharePercent = 70.6,
                        },
                    },
                    FrequencyBreakdown = new List<DonationReportSubscriptionFrequencyRow>
                    {
                        new DonationReportSubscriptionFrequencyRow
                        {
                            FrequencyLabel = "1M",
                            SubscriptionCount = 50,
                            TotalPaidAmount = 12000,
                            SubscriptionSharePercent = 58.8,
                            AverageDonationAmount = 24,
                            ExpectedUpcomingAmount = 3600,
                        },
                    },
                    Subscriptions = new List<DonationReportSubscriptionRow>
                    {
                        new DonationReportSubscriptionRow
                        {
                            SubscriptionId = 42,
                            PublicId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                            StatusKey = "Active",
                            StatusLabel = "Ativa",
                            FrequencyKey = "1M",
                            Frequency = "1M",
                            Created = new DateTime(2026, 1, 15),
                            PaidDonationCount = 5,
                            TotalPaidAmount = 125,
                        },
                    },
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
                        DistinctDonorCount = 3100,
                        MedianPaidAmount = 25,
                        Donors = new List<DonationReportDonorCampaignFoodBankRow>
                        {
                            new DonationReportDonorCampaignFoodBankRow
                            {
                                CampaignKey = "42",
                                CampaignName = "2026",
                                FoodBankId = 1,
                                FoodBankName = "Lisboa",
                                DistinctDonorCount = 1500,
                            },
                        },
                        Subscriptions = new DonationReportSubscriptionSection
                        {
                            TotalPaidAmount = 15000,
                            PaidDonationCount = 420,
                            SubscriptionCount = 85,
                            ForecastPeriodStart = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc),
                            ForecastPeriodEnd = new DateTime(2026, 12, 31),
                            StatusBreakdown = new List<DonationReportSubscriptionStatusRow>
                            {
                                new DonationReportSubscriptionStatusRow
                                {
                                    StatusLabel = "Ativa",
                                    Count = 60,
                                    SharePercent = 70.6,
                                },
                            },
                            FrequencyBreakdown = new List<DonationReportSubscriptionFrequencyRow>
                            {
                                new DonationReportSubscriptionFrequencyRow
                                {
                                    FrequencyLabel = "1M",
                                    SubscriptionCount = 50,
                                    TotalPaidAmount = 12000,
                                    SubscriptionSharePercent = 58.8,
                                    AverageDonationAmount = 24,
                                    ExpectedUpcomingAmount = 3600,
                                },
                            },
                        },
                        UserLogins = new DonationReportUserLoginSection
                        {
                            TotalLogins = 1200,
                            TotalRegisteredUsers = 450,
                            Providers = new List<DonationReportUserLoginProviderRow>
                            {
                                new DonationReportUserLoginProviderRow
                                {
                                    ProviderKey = "Google",
                                    ProviderDisplayName = "Google",
                                    LoginCount = 800,
                                    RegisteredUserCount = 300,
                                },
                                new DonationReportUserLoginProviderRow
                                {
                                    ProviderKey = "Password",
                                    ProviderDisplayName = "Palavra-passe",
                                    LoginCount = 400,
                                    RegisteredUserCount = 150,
                                },
                            },
                        },
                    },
                    Options = new List<DonationReportCampaignFilterOption>
                    {
                        new DonationReportCampaignFilterOption
                        {
                            Key = DonationReportFilterPayload.AllCampaignsKey,
                            Label = "Todas as campanhas",
                        },
                        new DonationReportCampaignFilterOption { Key = "42", Label = "2026" },
                    },
                    Campaigns = new List<DonationReportCampaignDetail>
                    {
                        new DonationReportCampaignDetail
                        {
                            CampaignKey = "42",
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
                            DistinctDonorCount = 3100,
                            MedianPaidAmount = 25,
                            Donors = new List<DonationReportDonorCampaignFoodBankRow>
                            {
                                new DonationReportDonorCampaignFoodBankRow
                                {
                                    CampaignKey = "42",
                                    CampaignName = "2026",
                                    FoodBankId = 1,
                                    FoodBankName = "Lisboa",
                                    DistinctDonorCount = 1500,
                                },
                            },
                        },
                    },
                    Comparison = new DonationReportCampaignComparison
                    {
                        CampaignLabels = new List<string> { "2025", "2026" },
                        CampaignTotals = new List<double> { 98000, 125000 },
                        CampaignTotalsPeriodoOficial = new List<double> { 90000, 110000 },
                        CampaignTotalsForaPeriodoOficial = new List<double> { 8000, 15000 },
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
                        CampaignSubscriptionCounts = new List<int> { 42, 55 },
                        SubscriptionCountByStatusSeries = new List<DonationReportSeriesRow>
                        {
                            new DonationReportSeriesRow { Label = "Ativa", Values = new[] { 30.0, 40.0 } },
                            new DonationReportSeriesRow { Label = "Captura", Values = new[] { 5.0, 8.0 } },
                            new DonationReportSeriesRow { Label = "Criada", Values = new[] { 4.0, 4.0 } },
                            new DonationReportSeriesRow { Label = "Inativa", Values = new[] { 2.0, 2.0 } },
                            new DonationReportSeriesRow { Label = "Erro", Values = new[] { 1.0, 1.0 } },
                        },
                        CampaignDonationCounts = new List<int> { 3800, 4412 },
                        DonationCountByStatusSeries = new List<DonationReportSeriesRow>
                        {
                            new DonationReportSeriesRow { Label = "Pago", Values = new[] { 3600.0, 4200.0 } },
                            new DonationReportSeriesRow { Label = "Não pago", Values = new[] { 150.0, 180.0 } },
                            new DonationReportSeriesRow { Label = "A aguardar pagamento", Values = new[] { 40.0, 30.0 } },
                            new DonationReportSeriesRow { Label = "Erro de pagamento", Values = new[] { 10.0, 2.0 } },
                        },
                    },
                    PeriodoOficialOptions = new List<DonationReportCampaignFilterOption>
                    {
                        new DonationReportCampaignFilterOption
                        {
                            Key = DonationReportFilterPayload.PeriodoOficialAllKey,
                            Label = "Todas as doações",
                        },
                        new DonationReportCampaignFilterOption
                        {
                            Key = DonationReportFilterPayload.PeriodoOficialTrueKey,
                            Label = "Período oficial",
                        },
                        new DonationReportCampaignFilterOption
                        {
                            Key = DonationReportFilterPayload.PeriodoOficialFalseKey,
                            Label = "Fora do período oficial",
                        },
                    },
                },
            };
        }
    }
}
