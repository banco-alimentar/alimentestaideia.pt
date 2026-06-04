// -----------------------------------------------------------------------
// <copyright file="SubscriptionWebhookTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for Easypay subscription generic webhooks.
    /// </summary>
    public class SubscriptionWebhookTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string JsonMediaType = "application/json";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionWebhookTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public SubscriptionWebhookTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// subscription_create webhook marks the subscription as active on success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_SubscriptionCreate_ActivatesSubscription()
        {
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.SubscriptionSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedCreatedSubscriptionAsync(scope.ServiceProvider);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericSubscriptionCreateNotification(
                seed.TransactionKey,
                success: true);

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscription = await context.Subscriptions.AsNoTracking()
                .FirstAsync(s => s.Id == seed.SubscriptionId);
            Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        }

        /// <summary>
        /// subscription_create webhook marks the subscription as error on failure.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_SubscriptionCreate_MarksErrorOnFailure()
        {
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.SubscriptionSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedCreatedSubscriptionAsync(scope.ServiceProvider);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericSubscriptionCreateNotification(
                seed.TransactionKey,
                success: false);

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscription = await context.Subscriptions.AsNoTracking()
                .FirstAsync(s => s.Id == seed.SubscriptionId);
            Assert.Equal(SubscriptionStatus.Error, subscription.Status);
        }

        /// <summary>
        /// subscription_capture webhook updates the recurring capture payment status.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_SubscriptionCapture_UpdatesExistingPayment()
        {
            var captureDate = DateTime.UtcNow;
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.SubscriptionCaptureSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedSubscriptionWithCapturePaymentAsync(
                    scope.ServiceProvider,
                    captureDate);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericSubscriptionCaptureNotification(
                seed.EasyPayId,
                seed.TransactionKey,
                captureDate,
                success: true);

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var payment = await context.Payments
                .Cast<CreditCardPayment>()
                .AsNoTracking()
                .FirstAsync(p => p.TransactionKey == seed.TransactionKey && p.Created.Date == captureDate.Date);
            Assert.Equal("Success", payment.Status);
            var captureDonationId = await context.Payments
                .Where(p => p.Id == payment.Id)
                .Select(p => p.Donation.Id)
                .FirstAsync();
            Assert.Equal(seed.CaptureDonationId, captureDonationId);
            Assert.NotEqual(seed.InitialDonationId, seed.CaptureDonationId);
        }

        /// <summary>
        /// subscription_capture webhook creates a new recurring donation when no capture payment exists yet.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_SubscriptionCapture_CreatesRecurringDonation()
        {
            var captureDate = DateTime.UtcNow;
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.SubscriptionRecurringCaptureSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForRecurringCaptureAsync(
                    scope.ServiceProvider,
                    captureDate);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericSubscriptionCaptureNotification(
                seed.EasyPayId,
                seed.TransactionKey,
                captureDate,
                success: true);

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donationCount = await context.SubscriptionDonations
                .CountAsync(sd => sd.Subscription.Id == seed.SubscriptionId);
            Assert.Equal(2, donationCount);

            var recurringDonationId = await context.SubscriptionDonations
                .Where(sd => sd.Subscription.Id == seed.SubscriptionId && sd.Donation.Id != seed.InitialDonationId)
                .Select(sd => sd.Donation.Id)
                .FirstAsync();
            Assert.NotEqual(seed.InitialDonationId, recurringDonationId);

            var recurringDonation = await context.Donations.AsNoTracking()
                .FirstAsync(d => d.Id == recurringDonationId);
            Assert.Equal(captureDate.Date, recurringDonation.DonationDate.Date);
        }

        private WebApplicationFactory<Program> CreateWebhookFactory()
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestMailConfiguration.AddTrackedStubMail(services);
                });
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ApiCertificateV3"] = IntegrationTestCredentials.ApiCertificateV3,
                        ["IsEmailEnabled"] = "false",
                    });
                });
            });
        }

        private async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string payload)
        {
            using var content = new StringContent(payload, Encoding.UTF8, JsonMediaType);
            return await client.PostAsync(path, content);
        }
    }
}
