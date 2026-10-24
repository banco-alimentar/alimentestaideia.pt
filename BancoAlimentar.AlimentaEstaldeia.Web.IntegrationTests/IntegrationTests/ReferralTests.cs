// -----------------------------------------------------------------------
// <copyright file="ReferralTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the referral landing page.
    /// </summary>
    public class ReferralTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public ReferralTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// An unknown referral code shows the inactive campaign message and stays on the referral page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_InvalidCode_ShowsInactiveCampaignMessage()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/Referral?text=invalid-referral-code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/Referral", response.RequestMessage?.RequestUri?.AbsolutePath);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Esta campanha de referência não está activa", html);
            Assert.Contains("/Donation", html);
        }

        /// <summary>
        /// A valid referral code redirects to the donation page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ValidCode_RedirectsToDonation()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            });

            var response = await client.GetAsync($"/Referral?text={referralSeed.Code}");

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            Assert.Equal("/Donation", response.RequestMessage?.RequestUri?.AbsolutePath);
        }

        /// <summary>
        /// Invalid and inactive links do not change opening counters or create opening events.
        /// </summary>
        /// <param name="inactiveCode">Whether to request an existing inactive campaign.</param>
        /// <returns>A task representing the operation.</returns>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Get_InvalidOrInactiveCode_DoesNotRecordLinkOpen(bool inactiveCode)
        {
            using var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed seed;
            int originalEventCount;
            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.FindAsync(seed.ReferralId);
                referral.Active = false;
                referral.LinkOpenCount = 3;
                context.ReferralLinkOpens.Add(new ReferralLinkOpen
                {
                    ReferralId = seed.ReferralId,
                    OpenedAtUtc = DateTime.UtcNow.AddDays(-1),
                });
                await context.SaveChangesAsync();
                originalEventCount = await context.ReferralLinkOpens.CountAsync();
            }

            using var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            string code = inactiveCode ? seed.Code : $"unknown{Guid.NewGuid():N}";

            var response = await client.GetAsync($"/Referral?text={code}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Contains("Esta campanha de referência não está activa", await response.Content.ReadAsStringAsync());
            using var assertScope = webFactory.Services.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingReferral = await assertContext.Referrals.AsNoTracking().SingleAsync(referral => referral.Id == seed.ReferralId);
            Assert.Equal(3, existingReferral.LinkOpenCount);
            Assert.Equal(originalEventCount, await assertContext.ReferralLinkOpens.CountAsync());
            Assert.Single(await assertContext.ReferralLinkOpens.Where(open => open.ReferralId == seed.ReferralId).ToListAsync());
        }
    }
}
