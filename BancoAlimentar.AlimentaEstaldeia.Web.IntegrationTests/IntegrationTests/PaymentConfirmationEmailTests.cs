// -----------------------------------------------------------------------
// <copyright file="PaymentConfirmationEmailTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using AngleSharp.Html.Parser;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;
    using Xunit;

    /// <summary>
    /// Exercises payment webhooks through the real confirmation email templates.
    /// </summary>
    public class PaymentConfirmationEmailTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string CampaignName = "Coração à Mesa: Alimente esta ideia!";
        private const string RegularPaymentSubject = "Banco Alimentar: Confirmacao do pagamento da sua doação regular";
        private const string SinglePaymentSubject = "Banco Alimentar: Confirmamos o pagamento da sua doação";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentConfirmationEmailTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public PaymentConfirmationEmailTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Paid donations render referral context in each confirmation template and omit it without a referral.
        /// </summary>
        /// <param name="paymentMethod">Payment method or recurring subscription capture.</param>
        /// <param name="wantsReceipt">Whether the donor requested an invoice.</param>
        /// <param name="hasReferral">Whether the donation belongs to a referral campaign.</param>
        /// <returns>A task representing the operation.</returns>
        [Theory]
        [InlineData("MBWay", false, true)]
        [InlineData("MBWay", true, true)]
        [InlineData("CreditCard", false, true)]
        [InlineData("CreditCard", true, true)]
        [InlineData("Subscription", false, true)]
        [InlineData("Subscription", true, true)]
        [InlineData("MBWay", false, false)]
        [InlineData("MBWay", true, false)]
        [InlineData("CreditCard", false, false)]
        [InlineData("CreditCard", true, false)]
        [InlineData("Subscription", false, false)]
        [InlineData("Subscription", true, false)]
        public async Task PaidWebhook_RendersCampaignInConfirmationEmail(string paymentMethod, bool wantsReceipt, bool hasReferral)
        {
            var tracker = new StubMailTracker();
            using var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Replace(ServiceDescriptor.Scoped<IMail>(provider => new CapturingMail(provider, tracker, wantsReceipt)));
                });
                builder.ConfigureAppConfiguration((context, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ApiCertificateV3"] = IntegrationTestCredentials.ApiCertificateV3,
                        ["IsEmailEnabled"] = bool.TrueString,
                        ["Email.ConfirmPaymentWithInvoice.Subject"] = SinglePaymentSubject,
                        ["Email.ConfirmPaymentNoInvoice.Subject"] = SinglePaymentSubject,
                        ["Email.Subscription.ConfirmPaymentWithInvoice.Subject"] = RegularPaymentSubject,
                        ["Email.ConfirmPaymentWithInvoice.Body.Path"] = "Email.ConfirmPaymentWithInvoice.htm",
                        ["Email.ConfirmPaymentNoInvoice.Body.Path"] = "Email.ConfirmPaymentNoInvoice.htm",
                        ["Email.Subscription.ConfirmPaymentWithInvoice.Body.Path"] = "Email.Subscription.ConfirmPaymentWithInvoice.htm",
                    });
                });
            });

            string payload;
            string recipient;
            int donationId;
            bool isSubscription = paymentMethod == "Subscription";
            using (var scope = webFactory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Donation donation;
                if (isSubscription)
                {
                    var captureDate = DateTime.UtcNow;
                    var seed = await IntegrationTestDataSeeder.SeedSubscriptionWithCapturePaymentAsync(scope.ServiceProvider, captureDate);
                    donation = await context.Donations.Include(value => value.User).SingleAsync(value => value.Id == seed.CaptureDonationId);
                    payload = EasyPayWebhookPayloadBuilder.BuildGenericSubscriptionCaptureNotification(
                        seed.EasyPayId,
                        seed.TransactionKey,
                        captureDate);
                }
                else
                {
                    var publicId = Guid.NewGuid();
                    var seed = paymentMethod == "MBWay"
                        ? await IntegrationTestDataSeeder.SeedPendingDonationWithMBWayAsync(scope.ServiceProvider, publicId)
                        : await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(scope.ServiceProvider, publicId);
                    donation = seed.Donation;
                    payload = paymentMethod == "MBWay"
                        ? EasyPayWebhookPayloadBuilder.BuildMBWayPaymentNotification(publicId, seed.TransactionKey, seed.EasyPayId)
                        : EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(publicId, seed.TransactionKey, seed.EasyPayId);
                }

                if (hasReferral)
                {
                    var referral = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
                    donation.ReferralEntity = await context.Referrals.FindAsync(referral.ReferralId);
                    donation.ReferralEntity.Name = CampaignName;
                }

                donation.WantsReceipt = wantsReceipt;
                await context.SaveChangesAsync();
                donationId = donation.Id;
                recipient = donation.User.Email;
            }

            using var client = webFactory.CreateClient();
            using var content = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(isSubscription ? "/easypay/generic" : "/easypay/payment", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var assertScope = webFactory.Services.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var paidDonation = await assertContext.Donations.AsNoTracking().SingleAsync(value => value.Id == donationId);
            Assert.Equal(PaymentStatus.Payed, paidDonation.PaymentStatus);
            Assert.Equal(1, tracker.SendMailCalls);
            Assert.Equal(wantsReceipt ? 1 : 0, tracker.InvoiceEmailsSent);
            Assert.Equal(recipient, tracker.LastRecipient);

            string expectedSubject = isSubscription && wantsReceipt ? RegularPaymentSubject : SinglePaymentSubject;
            if (isSubscription && wantsReceipt && hasReferral)
            {
                expectedSubject += $" no âmbito da campanha {CampaignName}";
            }

            Assert.Equal(expectedSubject, tracker.LastSubject);
            var document = new HtmlParser().ParseDocument(tracker.LastBody);
            var thankYou = Assert.Single(
                document.QuerySelectorAll("font"),
                element => element.TextContent.StartsWith("Muito obrigado", StringComparison.Ordinal));
            string expectedThankYou = !wantsReceipt
                ? "Muito obrigado pela sua participação nesta campanha"
                : isSubscription
                    ? "Muito obrigado pela sua doação regular ao Banco Alimentar."
                    : "Muito obrigado pela sua participação na campanha do Banco Alimentar.";
            Assert.Equal(expectedThankYou, thankYou.TextContent);
            Assert.Contains(paymentMethod == "MBWay" ? "MBWay" : "Cartão de Crédito", document.Body.TextContent);
            if (hasReferral)
            {
                var campaign = Assert.Single(document.QuerySelectorAll("b"), element => element.TextContent == CampaignName);
                Assert.Equal(thankYou.Closest("td"), campaign.Closest("td"));
                string thankYouText = thankYou.Closest("td").TextContent;
                Assert.True(thankYouText.IndexOf(CampaignName, StringComparison.Ordinal) > thankYouText.IndexOf(expectedThankYou, StringComparison.Ordinal));
                Assert.Contains("no âmbito da campanha", thankYouText);
            }
            else
            {
                Assert.DoesNotContain(CampaignName, document.Body.TextContent);
                Assert.DoesNotContain("no âmbito da campanha", document.Body.TextContent);
            }

            Assert.DoesNotContain("{referralCampaignName}", tracker.LastBody);
            Assert.DoesNotContain("{PaymentSystem}", tracker.LastBody);
            Assert.DoesNotContain("{publicSubscriptionId}", tracker.LastBody);
            Assert.DoesNotContain("{invoiceDownloadUrl}", tracker.LastBody);
            if (wantsReceipt)
            {
                Assert.NotNull(document.QuerySelector("a[href*='GenerateInvoice?token=']"));
            }

            if (isSubscription && wantsReceipt)
            {
                Assert.NotNull(document.QuerySelector("a[href*='/Subscriptions/Details?publicId=']"));
            }

            using var duplicateContent = new System.Net.Http.StringContent(payload, Encoding.UTF8, "application/json");
            var duplicateResponse = await client.PostAsync(isSubscription ? "/easypay/generic" : "/easypay/payment", duplicateContent);
            Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
            Assert.Equal(1, tracker.SendMailCalls);
        }

        private sealed class CapturingMail : Mail
        {
            private readonly StubMailTracker tracker;
            private readonly bool wantsReceipt;

            public CapturingMail(IServiceProvider services, StubMailTracker tracker, bool wantsReceipt)
                : base(
                    services.GetRequiredService<IViewRenderService>(),
                    services.GetRequiredService<IWebHostEnvironment>(),
                    services.GetRequiredService<IConfiguration>(),
                    services.GetRequiredService<IStringLocalizerFactory>(),
                    services.GetRequiredService<IFeatureManager>(),
                    services.GetRequiredService<TelemetryClient>(),
                    services.GetRequiredService<IWebHostEnvironment>(),
                    services.GetRequiredService<NifApiValidator>(),
                    services.GetRequiredService<IInvoiceDownloadTokenService>(),
                    services.GetRequiredService<IServiceScopeFactory>())
            {
                this.tracker = tracker;
                this.wantsReceipt = wantsReceipt;
            }

            public override bool SendMail(
                string body,
                string subject,
                string mailTo,
                Stream stream,
                string attachmentName,
                IConfiguration configuration,
                string userId = null,
                int? paymentId = null)
            {
                Assert.Equal(this.wantsReceipt, stream != null);
                if (this.wantsReceipt)
                {
                    Assert.EndsWith(".pdf", attachmentName, StringComparison.OrdinalIgnoreCase);
                }

                this.tracker.RecordSendMail(body, subject, mailTo);
                stream?.Dispose();
                return true;
            }

            protected override Task<(Invoice Invoice, Stream PdfFile, InvoiceStatusResult InvoiceStatusResult)> GenerateInvoice(
                string publicId,
                IUnitOfWork context,
                Tenant tenant,
                TelemetryClient telemetryClient)
            {
                this.tracker.RecordInvoiceEmail();
                return Task.FromResult<(Invoice, Stream, InvoiceStatusResult)>((
                    new Invoice { Number = "TEST-REFERRAL" },
                    new MemoryStream(),
                    InvoiceStatusResult.GeneratedOk));
            }
        }
    }
}
