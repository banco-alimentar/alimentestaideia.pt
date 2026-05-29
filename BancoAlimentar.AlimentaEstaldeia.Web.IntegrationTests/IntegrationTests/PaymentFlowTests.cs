// -----------------------------------------------------------------------
// <copyright file="PaymentFlowTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
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
                    services.RemoveAll(typeof(IMail));
                    services.AddScoped<IMail, StubMail>();
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

            var paymentRedirectClient = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var paymentResponse = await paymentRedirectClient.GetAsync($"/Payment?publicId={publicId}");
            Assert.Equal(HttpStatusCode.Redirect, paymentResponse.StatusCode);
            Assert.Contains("/Thanks", paymentResponse.Headers.Location?.OriginalString, StringComparison.OrdinalIgnoreCase);

            var thanksResponse = await client.GetAsync($"/Thanks?publicId={publicId}");
            thanksResponse.EnsureSuccessStatusCode();
            var thanksHtml = await thanksResponse.Content.ReadAsStringAsync();
            Assert.Contains(publicId.ToString(), thanksHtml);
        }
    }
}
