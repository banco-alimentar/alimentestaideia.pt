// -----------------------------------------------------------------------
// <copyright file="SubscriptionForecastHelperTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="SubscriptionForecastHelper"/>.
    /// </summary>
    public class SubscriptionForecastHelperTests
    {
        /// <summary>
        /// Monthly subscriptions should include one donation per month in the forecast window.
        /// </summary>
        [Fact]
        public void CountUpcomingDonations_MonthlyFrequency_CountsScheduledDonations()
        {
            DateTime forecastStart = new DateTime(2026, 6, 2);
            DateTime forecastEnd = new DateTime(2026, 8, 31);
            DateTime lastDonation = new DateTime(2026, 5, 2);

            int count = SubscriptionForecastHelper.CountUpcomingDonations(
                forecastStart,
                forecastEnd,
                new DateTime(2027, 1, 1),
                lastDonation,
                new DateTime(2026, 1, 2),
                "1M");

            Assert.Equal(3, count);
        }

        /// <summary>
        /// Weekly subscriptions should include one donation per week in the forecast window.
        /// </summary>
        [Fact]
        public void CountUpcomingDonations_WeeklyFrequency_CountsScheduledDonations()
        {
            DateTime forecastStart = new DateTime(2026, 6, 2);
            DateTime forecastEnd = new DateTime(2026, 6, 23);
            DateTime lastDonation = new DateTime(2026, 6, 1);

            int count = SubscriptionForecastHelper.CountUpcomingDonations(
                forecastStart,
                forecastEnd,
                new DateTime(2027, 1, 1),
                lastDonation,
                new DateTime(2026, 1, 1),
                "1W");

            Assert.Equal(3, count);
        }

        /// <summary>
        /// Forecasts should stop at subscription expiration.
        /// </summary>
        [Fact]
        public void CountUpcomingDonations_StopsAtSubscriptionExpiration()
        {
            DateTime forecastStart = new DateTime(2026, 6, 2);
            DateTime forecastEnd = new DateTime(2026, 12, 31);
            DateTime lastDonation = new DateTime(2026, 5, 2);

            int count = SubscriptionForecastHelper.CountUpcomingDonations(
                forecastStart,
                forecastEnd,
                new DateTime(2026, 7, 2),
                lastDonation,
                new DateTime(2026, 1, 2),
                "1M");

            Assert.Equal(2, count);
        }

        /// <summary>
        /// Forecast horizon should use campaign end when report end is already in the past.
        /// </summary>
        [Fact]
        public void ResolveForecastPeriodEnd_UsesCampaignEnd_WhenReportEndIsInThePast()
        {
            DateTime forecastStart = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
            var campaign = new Campaign
            {
                End = new DateTime(2026, 12, 31, 23, 59, 59),
                ReportEnd = new DateTime(2026, 5, 31, 23, 59, 59),
            };

            DateTime? forecastEnd = SubscriptionForecastHelper.ResolveForecastPeriodEnd(campaign, forecastStart);

            Assert.Equal(campaign.End, forecastEnd);
        }

        /// <summary>
        /// Forecast horizon should use report end when it extends beyond campaign end.
        /// </summary>
        [Fact]
        public void ResolveForecastPeriodEnd_UsesReportEnd_WhenReportEndExtendsCampaign()
        {
            DateTime forecastStart = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
            var campaign = new Campaign
            {
                End = new DateTime(2026, 6, 30, 23, 59, 59),
                ReportEnd = new DateTime(2026, 12, 31, 23, 59, 59),
            };

            DateTime? forecastEnd = SubscriptionForecastHelper.ResolveForecastPeriodEnd(campaign, forecastStart);

            Assert.Equal(campaign.ReportEnd, forecastEnd);
        }

        /// <summary>
        /// Capture subscriptions should be included in forecast eligibility.
        /// </summary>
        [Fact]
        public void IsForecastEligibleStatus_IncludesActiveAndCapture()
        {
            Assert.True(SubscriptionForecastHelper.IsForecastEligibleStatus(SubscriptionStatus.Active));
            Assert.True(SubscriptionForecastHelper.IsForecastEligibleStatus(SubscriptionStatus.Capture));
            Assert.False(SubscriptionForecastHelper.IsForecastEligibleStatus(SubscriptionStatus.Created));
            Assert.False(SubscriptionForecastHelper.IsForecastEligibleStatus(SubscriptionStatus.Inactive));
            Assert.False(SubscriptionForecastHelper.IsForecastEligibleStatus(SubscriptionStatus.Error));
        }
    }
}
