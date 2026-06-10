// -----------------------------------------------------------------------
// <copyright file="SubscriptionForecastHelperTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
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
    }
}
