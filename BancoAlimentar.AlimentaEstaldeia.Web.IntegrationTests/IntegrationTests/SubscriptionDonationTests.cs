// -----------------------------------------------------------------------
// <copyright file="SubscriptionDonationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for authenticated subscription donation checkout.
    /// </summary>
    public class SubscriptionDonationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string CheckoutUrl = "https://checkout.integration.test/subscription";
        private const string UserEmail = "subscription-donor@integration.test";
        private const string UserPassword = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionDonationTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public SubscriptionDonationTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Authenticated user starts a subscription donation and is redirected to the stub Easypay checkout.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task AuthenticatedUser_SubscriptionDonation_StartsCheckoutWithStubEasyPay()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSubscriptionCheckout(services, CheckoutUrl);
                });
            });

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(scope.ServiceProvider, UserEmail, UserPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                UserEmail,
                UserPassword,
                allowAutoRedirect: false);

            var donationPage = await client.GetAsync("/Donation");
            donationPage.EnsureSuccessStatusCode();
            var donationContent = await HtmlHelpers.GetDocumentAsync(donationPage);

            var donationResponse = await client.SendAsync(
                (AngleSharp.Html.Dom.IHtmlFormElement)donationContent.QuerySelector("form[id='donationForm']"),
                (AngleSharp.Html.Dom.IHtmlInputElement)donationContent.QuerySelector("input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1,2:1,3:1,4:1,5:1,6:1",
                    ["FoodBankId"] = "1",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Subscription Donor Co",
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                    ["IsSubscriptionEnabled"] = "true",
                    ["SubscriptionFrequencySelected"] = "1M",
                });

            Assert.Equal(HttpStatusCode.Redirect, donationResponse.StatusCode);
            Assert.Contains("/SubscriptionPayment", donationResponse.Headers.Location?.ToString());

            var subscriptionPaymentResponse = await client.GetAsync(donationResponse.Headers.Location);
            subscriptionPaymentResponse.EnsureSuccessStatusCode();
            var paymentContent = await HtmlHelpers.GetDocumentAsync(subscriptionPaymentResponse);
            var antiForgeryToken = paymentContent.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
            var donationId = paymentContent.QuerySelector("input[name='donationId']")?.GetAttribute("value");
            var frequency = paymentContent.QuerySelector("input[name='FrequencyStringValue']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));
            Assert.False(string.IsNullOrEmpty(donationId));
            Assert.False(string.IsNullOrEmpty(frequency));

            using var checkoutRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiForgeryToken,
                ["donationId"] = donationId,
                ["FrequencyStringValue"] = frequency,
            });
            var checkoutResponse = await client.PostAsync("/SubscriptionPayment?handler=CreditCard", checkoutRequest);

            Assert.Equal(HttpStatusCode.Redirect, checkoutResponse.StatusCode);
            Assert.Equal(CheckoutUrl, checkoutResponse.Headers.Location?.ToString());

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscription = await context.Subscriptions
                .Include(s => s.InitialDonation)
                .OrderByDescending(s => s.Id)
                .FirstAsync();
            Assert.Equal(SubscriptionStatus.Created, subscription.Status);
            Assert.Equal(CheckoutUrl, subscription.Url);
            Assert.NotNull(subscription.InitialDonation);
        }
    }
}
