// -----------------------------------------------------------------------
// <copyright file="CampaignRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="CampaignRepository"/>.
    /// </summary>
    public class CampaignRepositoryTests
    {
        private readonly CampaignRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignRepositoryTests"/> class.
        /// </summary>
        public CampaignRepositoryTests()
        {
            var servicesFixture = new ServicesFixture();
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<CampaignRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Returns the default campaign when no time-bound campaign is active.
        /// </summary>
        [Fact]
        public void CanGetCurrentCampaign()
        {
            var result = this.repository.GetCurrentCampaign();

            Assert.NotNull(result);
            Assert.NotEmpty(result.ProductCatalogues);
            Assert.True(result.IsDefaultCampaign);
        }

        /// <summary>
        /// Prefers a time-bound active campaign over the default campaign.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetCurrentCampaignReturnsActiveTimedCampaignOverDefault()
        {
            var product = await this.context.ProductCatalogues.FirstAsync();
            var timedCampaign = new Campaign
            {
                Number = $"TIMED-{Guid.NewGuid():N}",
                Start = DateTime.Now.AddDays(-1),
                End = DateTime.Now.AddDays(30),
                ReportEnd = DateTime.Now.AddDays(30),
                IsDefaultCampaign = false,
                ProductCatalogues = new[] { product },
            };
            this.context.Campaigns.Add(timedCampaign);
            await this.context.SaveChangesAsync();

            var result = this.repository.GetCurrentCampaign();

            Assert.NotNull(result);
            Assert.False(result.IsDefaultCampaign);
            Assert.Equal(timedCampaign.Number, result.Number);
        }

        /// <summary>
        /// Resolves the timed campaign that was active on a historical date.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetCampaignForDateReturnsTimedCampaignForHistoricalDate()
        {
            var product = await this.context.ProductCatalogues.FirstAsync();
            var timedCampaign = new Campaign
            {
                Number = $"HIST-{Guid.NewGuid():N}",
                Start = new DateTime(2020, 1, 1),
                End = new DateTime(2020, 12, 31, 23, 59, 59),
                ReportEnd = new DateTime(2020, 12, 31, 23, 59, 59),
                IsDefaultCampaign = false,
                ProductCatalogues = new[] { product },
            };
            this.context.Campaigns.Add(timedCampaign);
            await this.context.SaveChangesAsync();

            var result = this.repository.GetCampaignForDate(new DateTime(2020, 6, 15));

            Assert.NotNull(result);
            Assert.Equal(timedCampaign.Id, result.Id);
        }
    }
}
