// -----------------------------------------------------------------------
// <copyright file="AdminSubscriptionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
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
            int firstRowIndex = html.IndexOf("<tr", headerEndIndex, System.StringComparison.Ordinal);
            int rowEndIndex = html.IndexOf("</tr>", firstRowIndex, System.StringComparison.Ordinal);
            string firstRow = System.Net.WebUtility.HtmlDecode(html.Substring(firstRowIndex, rowEndIndex - firstRowIndex));
            Assert.Matches(@"5(?:[.,]00)?\s*€", firstRow);
        }

        /// <summary>
        /// Checks that the admin subscription list highlights a mismatch between donation values and paid total.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_Subscriptions_HighlightsDonationTotalMismatch()
        {
            // Arrange
            string donorEmail = $"integration-subscription-mismatch-{Guid.NewGuid():N}@test.com";
            IntegrationTestDataSeeder.ActiveSubscriptionSeed seed;
            using (var scope = this.factory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    donorEmail,
                    Password);

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var subscription = await context.Subscriptions
                    .Include(value => value.InitialDonation)
                    .SingleAsync(value => value.Id == seed.SubscriptionId);
                subscription.InitialDonation.PaymentStatus = PaymentStatus.WaitingPayment;

                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);
                await context.SaveChangesAsync();
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
            int emailIndex = html.IndexOf(donorEmail, StringComparison.Ordinal);
            Assert.True(emailIndex >= 0);
            int rowStartIndex = html.LastIndexOf("<tr", emailIndex, StringComparison.Ordinal);
            int rowEndIndex = html.IndexOf("</tr>", emailIndex, StringComparison.Ordinal);
            string row = html.Substring(rowStartIndex, rowEndIndex - rowStartIndex);
            Assert.Contains("table-danger", row);
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
                $"href=\"https://backoffice.test.easypay.pt/subscription/{seed.EasyPaySubscriptionId}\"",
                html);
            Assert.Contains("View in Easypay", html);
            Assert.Contains("Public id", html);
        }

        /// <summary>
        /// Checks that production back-office links use the production Easypay host.
        /// </summary>
        [Fact]
        public void EasyPayBackOfficeLinks_UsesProductionHostOutsideDevelopment()
        {
            var environment = new TestHostEnvironment
            {
                EnvironmentName = Environments.Production,
            };

            Assert.Equal(
                "https://bo.easypay.pt/subscription/subscription-id",
                EasyPayBackOfficeLinks.BuildSubscriptionUrl("subscription-id", environment));
            Assert.Equal(
                "https://backoffice.easypay.pt/payments/v2/single/338300db-31e0-4ed0-bc63-0881d0befad3/payment-id",
                EasyPayBackOfficeLinks.BuildPaymentUrl("payment-id", environment));
        }

        /// <summary>
        /// Checks that the subscription details page displays the subscription and payment data returned by Easypay.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_SubscriptionDetails_ShowsEasypayProviderDataAndPayments()
        {
            // Arrange
            string donorEmail = $"integration-subscription-provider-{Guid.NewGuid():N}@test.com";
            string paymentId = Guid.NewGuid().ToString();
            IntegrationTestDataSeeder.ActiveSubscriptionSeed seed;
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Easypay:BaseUrl"] = "https://api.integration.test",
                        ["Easypay:AccountId"] = "integration-account",
                        ["Easypay:ApiKey"] = "integration-key",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSubscriptionDetails(
                        services,
                        Guid.NewGuid().ToString(),
                        paymentId);
                });
            });

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    donorEmail,
                    Password);
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var subscription = await context.Subscriptions.SingleAsync(value => value.Id == seed.SubscriptionId);
                subscription.EasyPaySubscriptionId = seed.EasyPaySubscriptionId;
                await context.SaveChangesAsync();
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                AdminEmail,
                Password);

            // Act
            var response = await client.GetAsync($"/Admin/Subscriptions/Details?id={seed.SubscriptionId}");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Easypay subscription data", html);
            Assert.Contains("Integration Customer", html);
            Assert.Contains(paymentId, html);
            Assert.Contains("Full Easypay response", html);
            Assert.Contains("<summary", html);
            Assert.Contains("Easypay payments", html);
            Assert.Contains("Payment transactions shown in Easypay", html);
            Assert.Contains("Donations in the local database", html);
            Assert.Contains("Total paid reported by Easypay", html);
            Assert.Contains("Local database", html);
            Assert.Contains("Easypay", html);
            Assert.Contains("table-success", html);
            Assert.DoesNotContain("The number of Easypay transactions differs", html);
        }

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public string ApplicationName { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

            public string ContentRootPath { get; set; } = string.Empty;

            public string EnvironmentName { get; set; } = string.Empty;
        }
    }
}
