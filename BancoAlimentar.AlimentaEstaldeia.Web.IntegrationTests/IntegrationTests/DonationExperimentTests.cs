// -----------------------------------------------------------------------
// <copyright file="DonationExperimentTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    /// <summary>
    /// Integration tests for Donation2 experiment routing.
    /// </summary>
    public class DonationExperimentTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationExperimentTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public DonationExperimentTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// A 100 percent allocation redirects Donation GET requests and preserves query values.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetDonation_WithFullAllocation_RedirectsToDonation2AndPreservesQuery()
        {
            var client = this.CreateClient(100);

            var response = await client.GetAsync("/Donation?referral=campaign-code");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Donation2?referral=campaign-code", response.Headers.Location?.ToString());
        }

        /// <summary>
        /// A zero percent allocation keeps Donation GET requests on the legacy page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetDonation_WithNoAllocation_ReturnsLegacyDonationPage()
        {
            var client = this.CreateClient(0);

            var response = await client.GetAsync("/Donation");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/Donation", response.RequestMessage?.RequestUri?.AbsolutePath);
        }

        private System.Net.Http.HttpClient CreateClient(int trafficPercentage)
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Donation2Experiment:TrafficPercentage"] = trafficPercentage.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["FeatureManagement:EnableMaintenance"] = "false",
                    });
                });
            }).CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }
    }
}