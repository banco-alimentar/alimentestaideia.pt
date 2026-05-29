// -----------------------------------------------------------------------
// <copyright file="WebhookNotificationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    /// <summary>
    /// Integration tests for Easypay and legacy payment notification webhooks.
    /// </summary>
    public class WebhookNotificationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string JsonMediaType = "application/json";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebhookNotificationTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public WebhookNotificationTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Credit-card Easypay webhook marks the donation as paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_CompletesCreditCardDonation()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(
                    scope.ServiceProvider,
                    publicId);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);
        }

        /// <summary>
        /// Multibanco Easypay webhook marks the donation as paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_CompletesMultiBankDonation()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithMultiBankAsync(
                    scope.ServiceProvider,
                    publicId);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildMultiBankPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);
        }

        /// <summary>
        /// Duplicate Easypay payment webhooks remain idempotent for paid donations.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_IsIdempotentWhenPostedTwice()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(
                    scope.ServiceProvider,
                    publicId);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            var first = await this.PostJsonAsync(client, "/easypay/payment", payload);
            var second = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);
        }

        /// <summary>
        /// Generic Easypay webhook updates multibanco payment status to paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_MarksMultiBankDonationPaid()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithMultiBankAsync(
                    scope.ServiceProvider,
                    publicId);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericPaymentNotification(
                seed.EasyPayId,
                seed.TransactionKey);

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);
        }

        /// <summary>
        /// Legacy multibanco reminder endpoint rejects invalid API keys.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task LegacyPaymentNotification_RejectsInvalidApiKey()
        {
            var client = this.CreateWebhookFactory().CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var response = await client.GetAsync(
                $"/notifications/payment?multibankId=999999&key=definitely-not-{IntegrationTestCredentials.ApiCertificateV3}");

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private WebApplicationFactory<Program> CreateWebhookFactory()
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IMail));
                    services.AddScoped<IMail, StubMail>();
                });
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ApiCertificateV3"] = IntegrationTestCredentials.ApiCertificateV3,
                        ["Email.MultibancoReminder.Subject"] = "Integration multibanco reminder",
                        ["Email.MultibancoReminder.Body.Path"] = "Email.MultibancoReminder.htm",
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

        private async Task AssertDonationIsPaidAsync(WebApplicationFactory<Program> webFactory, Guid publicId)
        {
            using var scope = webFactory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.AsNoTracking().FirstAsync(d => d.PublicId == publicId);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
        }
    }
}
