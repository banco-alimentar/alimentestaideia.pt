// -----------------------------------------------------------------------
// <copyright file="ReferralTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
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
    }
}
