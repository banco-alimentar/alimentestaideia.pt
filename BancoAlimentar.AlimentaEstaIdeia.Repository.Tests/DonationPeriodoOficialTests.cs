// -----------------------------------------------------------------------
// <copyright file="DonationPeriodoOficialTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DonationPeriodoOficial"/>.
    /// </summary>
    public class DonationPeriodoOficialTests
    {
        /// <summary>
        /// Returns true when the donation date is inside the official period.
        /// </summary>
        [Fact]
        public void IsWithinOfficialPeriod_ReturnsTrue_WhenDonationDateIsInsideStartAndReportEnd()
        {
            var campaign = new Campaign
            {
                Start = new DateTime(2026, 5, 1, 0, 0, 0),
                ReportEnd = new DateTime(2026, 6, 30, 23, 59, 59),
            };

            bool result = DonationPeriodoOficial.IsWithinOfficialPeriod(
                new DateTime(2026, 6, 15, 12, 0, 0),
                campaign);

            Assert.True(result);
        }

        /// <summary>
        /// Treats campaign start and report end as inclusive boundaries.
        /// </summary>
        [Fact]
        public void IsWithinOfficialPeriod_ReturnsTrue_OnStartAndReportEndBoundaries()
        {
            var campaign = new Campaign
            {
                Start = new DateTime(2026, 5, 1, 0, 0, 0),
                ReportEnd = new DateTime(2026, 6, 30, 23, 59, 59),
            };

            Assert.True(DonationPeriodoOficial.IsWithinOfficialPeriod(campaign.Start, campaign));
            Assert.True(DonationPeriodoOficial.IsWithinOfficialPeriod(campaign.ReportEnd, campaign));
        }

        /// <summary>
        /// Returns false when the donation date is after report end.
        /// </summary>
        [Fact]
        public void IsWithinOfficialPeriod_ReturnsFalse_WhenDonationDateIsOutsideReportEnd()
        {
            var campaign = new Campaign
            {
                Start = new DateTime(2026, 5, 1, 0, 0, 0),
                ReportEnd = new DateTime(2026, 6, 30, 23, 59, 59),
            };

            bool result = DonationPeriodoOficial.IsWithinOfficialPeriod(
                new DateTime(2026, 7, 1, 0, 0, 0),
                campaign);

            Assert.False(result);
        }

        /// <summary>
        /// Returns false when no campaign is provided.
        /// </summary>
        [Fact]
        public void IsWithinOfficialPeriod_ReturnsFalse_WhenCampaignIsNull()
        {
            bool result = DonationPeriodoOficial.IsWithinOfficialPeriod(DateTime.UtcNow, (Campaign)null);

            Assert.False(result);
        }
    }
}
