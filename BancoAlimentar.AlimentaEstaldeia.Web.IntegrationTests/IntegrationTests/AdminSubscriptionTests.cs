// -----------------------------------------------------------------------
// <copyright file="AdminSubscriptionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the admin subscriptions page.
    /// </summary>
    public class AdminSubscriptionTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "integration-subscriptions-admin@test.com";
        private const string DonorEmail = "integration-subscriptions-donor@test.com";
        private const string Password = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminSubscriptionTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminSubscriptionTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Checks that the admin subscription list displays individual donation values.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_Subscriptions_ShowsIndividualDonationValues()
        {
            // Arrange
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    DonorEmail,
                    Password);
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password);

            // Act
            var response = await client.GetAsync("/Admin/Subscriptions");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Donation values", html);
            int valuesHeaderIndex = html.IndexOf("Donation values", System.StringComparison.Ordinal);
            int headerEndIndex = html.IndexOf("</thead>", valuesHeaderIndex, System.StringComparison.Ordinal);
            int firstRowIndex = html.IndexOf("<tr>", headerEndIndex, System.StringComparison.Ordinal);
            int rowEndIndex = html.IndexOf("</tr>", firstRowIndex, System.StringComparison.Ordinal);
            string firstRow = System.Net.WebUtility.HtmlDecode(html.Substring(firstRowIndex, rowEndIndex - firstRowIndex));
            Assert.Matches(@"5(?:[.,]00)?\s*€", firstRow);
        }

        /// <summary>
        /// Checks that the admin subscription details page links to the Easypay back office.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_SubscriptionDetails_ShowsEasypayBackofficeLink()
        {
            // Arrange
            string donorEmail = $"integration-subscription-details-donor-{Guid.NewGuid():N}@test.com";
            IntegrationTestDataSeeder.ActiveSubscriptionSeed seed;
            using (var scope = this.factory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    donorEmail,
                    Password);
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password);

            // Act
            var response = await client.GetAsync($"/Admin/Subscriptions/Details?id={seed.SubscriptionId}");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains(
                $"href=\"https://bo.easypay.pt/subscription/{seed.EasyPaySubscriptionId}\"",
                html);
            Assert.Contains("View in Easypay", html);
            Assert.Contains("Subscription ID", html);
        }
    }
}
