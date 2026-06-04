// -----------------------------------------------------------------------
// <copyright file="PaymentMethodFlowTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for starting PayPal and MBWay payment flows.
    /// </summary>
    public class PaymentMethodFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string PayPalApproveUrl = "https://paypal.integration.test/approve";
        private const string CreditCardCheckoutUrl = "https://checkout.integration.test/credit-card";
        private const string CreditCardPaymentId = "integration-cc-payment-id";
        private const string MultibancoPaymentId = "integration-mb-payment-id";
        private const string MultibancoEntity = "12345";
        private const string MultibancoReference = "987654321";
        private const string MbWayPaymentId = "integration-mbway-payment-id";
        private const string MbWayAlias = "integration-mbway-alias";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentMethodFlowTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public PaymentMethodFlowTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Anonymous donor starting PayPal checkout is redirected to the stub approve URL.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousDonation_PayPal_StartsCheckoutWithStubPayPal()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestPayPalConfiguration.AddStubPayPalCheckout(services, PayPalApproveUrl);
                });
            });

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var paymentPage = await this.StartAnonymousDonationAsync(client);
            var antiForgeryToken = paymentPage.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
            var donationId = paymentPage.QuerySelector("input[name='DonationId']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));
            Assert.False(string.IsNullOrEmpty(donationId));

            using var payPalRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiForgeryToken,
                ["DonationId"] = donationId,
                ["serviceReference"] = "196807050|test-ref",
                ["serviceAmount"] = "1",
            });
            var payPalResponse = await client.PostAsync("/Payment?handler=PayPal", payPalRequest);

            Assert.Equal(HttpStatusCode.Redirect, payPalResponse.StatusCode);
            Assert.Equal(PayPalApproveUrl, payPalResponse.Headers.Location?.ToString());
        }

        /// <summary>
        /// Anonymous donor starting MBWay payment is redirected to the MBWay payment page and a payment row is stored.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousDonation_MBWay_StartsPaymentPageWithStubEasyPay()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSinglePaymentCheckout(
                        services,
                        paymentId: MbWayPaymentId,
                        mbWayAlias: MbWayAlias);
                });
            });

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var paymentPage = await this.StartAnonymousDonationAsync(client);
            var antiForgeryToken = paymentPage.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
            var donationId = paymentPage.QuerySelector("input[name='DonationId']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));
            Assert.False(string.IsNullOrEmpty(donationId));

            using var mbWayRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiForgeryToken,
                ["DonationId"] = donationId,
                ["PhoneNumber"] = "912345678",
            });
            var mbWayResponse = await client.PostAsync("/Payment?handler=MbWay", mbWayRequest);

            Assert.Equal(HttpStatusCode.Redirect, mbWayResponse.StatusCode);
            Assert.Contains("/Payments/MBWayPayment", mbWayResponse.Headers.Location?.ToString());
            Assert.Contains(MbWayPaymentId, mbWayResponse.Headers.Location?.ToString());

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mbWayPayment = await context.MBWayPayments
                .OrderByDescending(p => p.Id)
                .FirstAsync();
            Assert.Equal(MbWayPaymentId, mbWayPayment.EasyPayPaymentId);
            Assert.Equal(MbWayAlias, mbWayPayment.Alias);

            var mbWayPageResponse = await client.GetAsync(mbWayResponse.Headers.Location);
            mbWayPageResponse.EnsureSuccessStatusCode();
            var mbWayHtml = await mbWayPageResponse.Content.ReadAsStringAsync();
            Assert.Contains("We await confirmation of payment MBWay", mbWayHtml);
            Assert.Contains("/Payments/MBWayPayment", mbWayResponse.Headers.Location?.ToString());
        }

        /// <summary>
        /// Anonymous donor starting Multibanco payment is redirected to the Multibanco page and a payment row is stored.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousDonation_Multibanco_StartsMultibancoPageWithStubEasyPay()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSinglePaymentCheckout(
                        services,
                        paymentId: MultibancoPaymentId,
                        paymentMethodType: "mb",
                        multibancoEntity: MultibancoEntity,
                        multibancoReference: MultibancoReference);
                });
            });

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var paymentPage = await this.StartAnonymousDonationAsync(client);
            var antiForgeryToken = paymentPage.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
            var donationId = paymentPage.QuerySelector("input[name='donationId']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));
            Assert.False(string.IsNullOrEmpty(donationId));

            using var multibancoRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiForgeryToken,
                ["donationId"] = donationId,
            });
            var multibancoResponse = await client.PostAsync("/Payment?handler=PayWithMultibanco", multibancoRequest);

            Assert.Equal(HttpStatusCode.Redirect, multibancoResponse.StatusCode);
            Assert.Contains("/Payments/Multibanco", multibancoResponse.Headers.Location?.ToString());

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var multiBankPayment = await context.Payments
                .OfType<MultiBankPayment>()
                .OrderByDescending(p => p.Id)
                .FirstAsync();
            Assert.Equal(MultibancoPaymentId, multiBankPayment.EasyPayPaymentId);
            var donation = await context.Donations.FirstAsync(d => d.Id == int.Parse(donationId));
            Assert.Equal(MultibancoEntity, donation.ServiceEntity);
            Assert.Equal(MultibancoReference, donation.ServiceReference);

            var multibancoPageResponse = await client.GetAsync(multibancoResponse.Headers.Location);
            multibancoPageResponse.EnsureSuccessStatusCode();
            var multibancoHtml = await multibancoPageResponse.Content.ReadAsStringAsync();
            Assert.Contains(MultibancoEntity, multibancoHtml);
            Assert.Contains(MultibancoReference, multibancoHtml);
        }

        /// <summary>
        /// Anonymous donor starting credit-card checkout is redirected to the stub checkout URL and a payment row is stored.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousDonation_CreditCard_StartsCheckoutWithStubEasyPay()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSinglePaymentCheckout(
                        services,
                        paymentId: CreditCardPaymentId,
                        paymentMethodType: "cc",
                        checkoutUrl: CreditCardCheckoutUrl);
                });
            });

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var paymentPage = await this.StartAnonymousDonationAsync(client);
            var antiForgeryToken = paymentPage.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
            var donationId = paymentPage.QuerySelector("input[name='donationId']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));
            Assert.False(string.IsNullOrEmpty(donationId));

            using var creditCardRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiForgeryToken,
                ["donationId"] = donationId,
            });
            var creditCardResponse = await client.PostAsync("/Payment?handler=CreditCard", creditCardRequest);

            Assert.Equal(HttpStatusCode.Redirect, creditCardResponse.StatusCode);
            Assert.Equal(CreditCardCheckoutUrl, creditCardResponse.Headers.Location?.ToString());

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var creditCardPayment = await context.CreditCardPayments
                .OrderByDescending(p => p.Id)
                .FirstAsync();
            Assert.Equal(CreditCardPaymentId, creditCardPayment.EasyPayPaymentId);
            Assert.Equal(CreditCardCheckoutUrl, creditCardPayment.Url);
        }

        /// <summary>
        /// PayPal return callback with a completed capture marks the donation as paid and redirects to Thanks.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousDonation_PayPalCapture_CompletesPaymentWithStubPayPal()
        {
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestPayPalConfiguration.AddStubPayPalCheckoutWithCapture(services, PayPalApproveUrl);
                });
            });

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var paymentPage = await this.StartAnonymousDonationAsync(client);
            var donationId = paymentPage.QuerySelector("input[name='DonationId']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(donationId));

            var captureResponse = await client.GetAsync(
                $"/Payment?handler=ReferencePayedViaPayPal&donationId={donationId}&paymentId=integration-payment&token=integration-token&payerId=integration-payer");

            Assert.Equal(HttpStatusCode.Redirect, captureResponse.StatusCode);
            Assert.Contains("/Thanks", captureResponse.Headers.Location?.ToString());

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.FirstAsync(d => d.Id == int.Parse(donationId));
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
        }

        private async Task<AngleSharp.Dom.IDocument> StartAnonymousDonationAsync(HttpClient client)
        {
            var donationPage = await client.GetAsync("/Donation");
            donationPage.EnsureSuccessStatusCode();
            var donationContent = await HtmlHelpers.GetDocumentAsync(donationPage);
            var email = $"payment-flow-{System.Guid.NewGuid():N}@integration.test";

            var donationResponse = await client.SendAsync(
                (IHtmlFormElement)donationContent.QuerySelector("form[id='donationForm']"),
                (IHtmlInputElement)donationContent.QuerySelector("input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1,2:1,3:1,4:1,5:1,6:1",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Payment Flow Donor",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Payment Flow Co",
                    ["Email"] = email,
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            Assert.Equal(HttpStatusCode.Redirect, donationResponse.StatusCode);
            Assert.Contains("/Payment", donationResponse.Headers.Location?.ToString());

            var paymentResponse = await client.GetAsync(donationResponse.Headers.Location);
            paymentResponse.EnsureSuccessStatusCode();
            return await HtmlHelpers.GetDocumentAsync(paymentResponse);
        }
    }
}
