// -----------------------------------------------------------------------
// <copyright file="WebhookNotificationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
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
        /// MBWay Easypay webhook marks the donation as paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_CompletesMBWayDonation()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithMBWayAsync(
                    scope.ServiceProvider,
                    publicId);
            }

            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildMBWayPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);
        }

        /// <summary>
        /// MBWay waiting page redirects to Thanks when the webhook already marked the donation paid,
        /// even if Easypay status lookup still reports failed (stale MBWay request).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task MBWayPayment_RedirectsToThanksWhenWebhookAlreadyCompleted()
        {
            var publicId = Guid.NewGuid();
            var paymentGuid = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSinglePaymentCheckout(
                        services,
                        paymentId: paymentGuid.ToString(),
                        singlePaymentLookupMethodStatus: "failed");
                });
            });

            IntegrationTestDataSeeder.PendingDonationSeed seed;
            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithMBWayAsync(
                    scope.ServiceProvider,
                    publicId,
                    paymentGuid.ToString());
            }

            var webhookClient = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildMBWayPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            var webhookResponse = await this.PostJsonAsync(webhookClient, "/easypay/payment", payload);
            Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var response = await client.GetAsync(
                $"/Payments/MBWayPayment?PublicId={publicId}&paymentId={paymentGuid}");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Thanks", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
            await this.AssertDonationIsPaidAsync(webFactory, publicId);
        }

        /// <summary>
        /// Payment webhook sends invoice confirmation email when email is enabled.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_SendsInvoiceEmailWhenEnabled()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory(enableEmail: true);
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

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(1, tracker.InvoiceEmailsSent);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(await context.PaymentNotifications.AnyAsync(
                p => p.Payment.Id == seed.PaymentId && p.NotificationType == NotificationType.Email));
        }

        /// <summary>
        /// Duplicate payment webhooks send at most one invoice email.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_InvoiceEmailIsIdempotentWhenPostedTwice()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory(enableEmail: true);
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

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(1, tracker.InvoiceEmailsSent);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await context.PaymentNotifications.CountAsync(p => p.Payment.Id == seed.PaymentId && p.NotificationType == NotificationType.Email));
        }

        /// <summary>
        /// Easypay payment webhook rejects notifications for unknown transaction keys.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_ReturnsForbidden_WhenDonationIsUnknown()
        {
            var client = this.CreateWebhookFactory().CreateClient();
            var unknownPublicId = Guid.NewGuid();
            var payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                unknownPublicId,
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Easypay payment webhook rejects malformed JSON bodies.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_ReturnsBadRequest_WhenPayloadIsMalformed()
        {
            var client = this.CreateWebhookFactory().CreateClient();
            using var content = new StringContent("{ not-valid-json", Encoding.UTF8, JsonMediaType);
            var response = await client.PostAsync("/easypay/payment", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Easypay generic webhook rejects notifications for unknown transaction keys.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_ReturnsForbidden_WhenPaymentIsUnknown()
        {
            var client = this.CreateWebhookFactory().CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericPaymentNotification(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        /// <summary>
        /// Legacy multibanco reminder endpoint sends email for pending payments with a valid API key.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task LegacyPaymentNotification_SendsReminderForPendingMultiBankPayment()
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

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var response = await client.GetAsync(
                $"/notifications/payment?multibankId={seed.PaymentId}&key={IntegrationTestCredentials.ApiCertificateV3}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(1, tracker.SendMailCalls);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(await context.PaymentNotifications.AnyAsync(
                p => p.Payment.Id == seed.PaymentId && p.NotificationType == NotificationType.Email));
        }

        /// <summary>
        /// Legacy multibanco reminder endpoint sends at most one email per payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task LegacyPaymentNotification_IsIdempotentWhenCalledTwice()
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

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var url = $"/notifications/payment?multibankId={seed.PaymentId}&key={IntegrationTestCredentials.ApiCertificateV3}";
            var first = await client.GetAsync(url);
            var second = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(1, tracker.SendMailCalls);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await context.PaymentNotifications.CountAsync(p => p.Payment.Id == seed.PaymentId && p.NotificationType == NotificationType.Email));
        }

        /// <summary>
        /// Legacy multibanco reminder endpoint skips email when the donation is already paid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task LegacyPaymentNotification_SkipsReminderWhenDonationAlreadyPaid()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithMultiBankAsync(
                    scope.ServiceProvider,
                    publicId);
                var seedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var donation = await seedContext.Donations.FirstAsync(d => d.Id == seed.Donation.Id);
                donation.PaymentStatus = PaymentStatus.Payed;
                donation.ConfirmedPayment = await seedContext.Payments.FirstAsync(p => p.Id == seed.PaymentId);
                await seedContext.SaveChangesAsync();
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            var response = await client.GetAsync(
                $"/notifications/payment?multibankId={seed.PaymentId}&key={IntegrationTestCredentials.ApiCertificateV3}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(0, tracker.SendMailCalls);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(await context.PaymentNotifications.AnyAsync(
                p => p.Payment.Id == seed.PaymentId && p.NotificationType == NotificationType.Email));
        }

        /// <summary>
        /// Forged transaction key is rejected before payment state changes.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_RejectsUnknownTransactionKey()
        {
            var client = this.CreateWebhookFactory().CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                Guid.NewGuid(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Invoice email is not sent when payment completion fails (unknown payment row).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_DoesNotSendInvoiceWhenPaymentCompletionFails()
        {
            var publicId = Guid.NewGuid();
            var webFactory = this.CreateWebhookFactory(enableEmail: true, permissiveVerifier: true);
            var client = webFactory.CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                publicId,
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var tracker = webFactory.Services.GetRequiredService<StubMailTracker>();
            Assert.Equal(0, tracker.InvoiceEmailsSent);
        }

        /// <summary>
        /// Tampered paid amount is rejected and donation stays unpaid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayPayment_RejectsAmountMismatch()
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
                seed.EasyPayId,
                amount: 0.01);

            var response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await this.AssertDonationIsWaitingPaymentAsync(webFactory, publicId);
        }

        /// <summary>
        /// Generic capture webhook rejects unknown transaction keys.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task EasyPayGeneric_RejectsUnknownTransactionKey()
        {
            var client = this.CreateWebhookFactory().CreateClient();
            var payload = EasyPayWebhookPayloadBuilder.BuildGenericPaymentNotification(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());

            var response = await this.PostJsonAsync(client, "/easypay/generic", payload);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private WebApplicationFactory<Program> CreateWebhookFactory(bool enableEmail = false, bool permissiveVerifier = false)
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestMailConfiguration.AddTrackedStubMail(services);
                    if (permissiveVerifier)
                    {
                        services.Replace(ServiceDescriptor.Scoped<IEasyPayWebhookVerifier, PermissiveEasyPayWebhookVerifier>());
                    }
                });
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ApiCertificateV3"] = IntegrationTestCredentials.ApiCertificateV3,
                        ["Email.MultibancoReminder.Subject"] = "Integration multibanco reminder",
                        ["Email.MultibancoReminder.Body.Path"] = "Email.MultibancoReminder.htm",
                        ["IsEmailEnabled"] = enableEmail ? "true" : "false",
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

        private async Task AssertDonationIsWaitingPaymentAsync(WebApplicationFactory<Program> webFactory, Guid publicId)
        {
            using var scope = webFactory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations.AsNoTracking().FirstAsync(d => d.PublicId == publicId);
            Assert.Equal(PaymentStatus.WaitingPayment, donation.PaymentStatus);
        }
    }
}
