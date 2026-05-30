// -----------------------------------------------------------------------
// <copyright file="PaymentFlowTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the donation payment to thanks page flow.
    /// </summary>
    public class PaymentFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string JsonMediaType = "application/json";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentFlowTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public PaymentFlowTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// After payment completes, Payment redirects to Thanks and Thanks renders successfully.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task PaidDonation_RedirectsFromPaymentToThanks()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.factory.WithWebHostBuilder(builder =>
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

            IntegrationTestDataSeeder.PendingDonationSeed seed;
            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(
                    scope.ServiceProvider,
                    publicId);
            }

            var client = webFactory.CreateClient();
            var webhookPayload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);
            using var webhookContent = new StringContent(webhookPayload, Encoding.UTF8, JsonMediaType);
            var webhookResponse = await client.PostAsync("/easypay/payment", webhookContent);
            Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

            using (var scope = webFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var donation = await context.Donations.AsNoTracking().FirstAsync(d => d.PublicId == publicId);
                Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            }

            var paymentRedirectClient = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var paymentResponse = await paymentRedirectClient.GetAsync($"/Payment?publicId={publicId}");
            Assert.True(
                paymentResponse.StatusCode == HttpStatusCode.Redirect || paymentResponse.StatusCode == HttpStatusCode.Found,
                $"Expected redirect to Thanks, got {paymentResponse.StatusCode}");
            Assert.Contains("/Thanks", paymentResponse.Headers.Location?.OriginalString, StringComparison.OrdinalIgnoreCase);

            var thanksResponse = await client.GetAsync($"/Thanks?publicId={publicId}");
            thanksResponse.EnsureSuccessStatusCode();
            var thanksHtml = await thanksResponse.Content.ReadAsStringAsync();
            Assert.Contains(publicId.ToString(), thanksHtml);
        }
    }
}
