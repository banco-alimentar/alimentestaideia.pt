// -----------------------------------------------------------------------
// <copyright file="EasyPayApiWebhookVerifierTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Configuration = Easypay.Rest.Client.Client.Configuration;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Xunit;

    /// <summary>
    /// Tests the production Easypay webhook verifier without calling the generated HTTP clients.
    /// </summary>
    public sealed class EasyPayApiWebhookVerifierTests : IDisposable
    {
        private const double DonationAmount = 10;
        private readonly ApplicationDbContext dbContext;
        private readonly Mock<ISinglePaymentApi> singlePaymentApi = new Mock<ISinglePaymentApi>();
        private readonly Mock<ISubscriptionPaymentApi> subscriptionPaymentApi = new Mock<ISubscriptionPaymentApi>();
        private readonly Mock<IEasyPayProviderApiFactory> apiFactory = new Mock<IEasyPayProviderApiFactory>();
        private readonly TelemetryConfiguration telemetryConfiguration;
        private readonly EasyPayApiWebhookVerifier verifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayApiWebhookVerifierTests"/> class.
        /// </summary>
        public EasyPayApiWebhookVerifierTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            this.dbContext = new ApplicationDbContext(options);

            var httpContext = new DefaultHttpContext();
            httpContext.SetTenant(new Tenant
            {
                Id = 1,
                Name = "integration-test",
                PaymentStrategy = PaymentStrategy.SharedPaymentProcessor,
                PublicId = Guid.NewGuid(),
            });
            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Easypay:BaseUrl"] = "https://easypay.integration.test",
                    ["Easypay:AccountId"] = "integration-account",
                    ["Easypay:ApiKey"] = "integration-key",
                })
                .Build();

            this.apiFactory
                .Setup(factory => factory.CreateSinglePaymentApi(It.IsAny<Configuration>()))
                .Returns(this.singlePaymentApi.Object);
            this.apiFactory
                .Setup(factory => factory.CreateSubscriptionPaymentApi(It.IsAny<Configuration>()))
                .Returns(this.subscriptionPaymentApi.Object);
            this.telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            this.verifier = new EasyPayApiWebhookVerifier(
                this.dbContext,
                httpContextAccessor,
                new EasyPayApiCredentialsFactory(configuration),
                this.apiFactory.Object,
                new TelemetryClient(this.telemetryConfiguration));
        }

        /// <summary>
        /// Missing transaction data is rejected before a provider lookup.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsMissingTransaction()
        {
            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                new TransactionNotificationRequest());

            Assert.False(result.IsValid);
            Assert.Equal("missing_transaction", result.FailureReason);
            this.apiFactory.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Unknown local transaction keys are rejected before a provider lookup.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsUnknownTransactionKey()
        {
            TransactionNotificationRequest notification = CreateNotification("unknown", Guid.NewGuid());

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(notification);

            Assert.False(result.IsValid);
            Assert.Equal("unknown_transaction_key", result.FailureReason);
            this.apiFactory.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Missing provider payment identifiers are rejected before a provider lookup.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsMissingPaymentId()
        {
            (Donation _, string transactionKey) = await this.SeedPendingDonationAsync();
            TransactionNotificationRequest notification = CreateNotification(transactionKey, Guid.Empty);

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(notification);

            Assert.False(result.IsValid);
            Assert.Equal("missing_payment_id", result.FailureReason);
            this.apiFactory.VerifyNoOtherCalls();
        }

        /// <summary>
        /// A matching paid provider payment is accepted and receives the request cancellation token.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_AcceptsMatchingPaidPayment()
        {
            (Donation donation, string transactionKey) = await this.SeedPendingDonationAsync();
            Guid paymentId = Guid.NewGuid();
            using var cancellationSource = new CancellationTokenSource();
            this.singlePaymentApi
                .Setup(api => api.SingleIdGetAsync(paymentId, It.IsAny<int>(), cancellationSource.Token))
                .ReturnsAsync(CreateProviderPayment(donation.PublicId, DonationAmount, "paid"));

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                CreateNotification(transactionKey, paymentId),
                cancellationSource.Token);

            Assert.True(result.IsValid);
            Assert.Null(result.FailureReason);
            this.singlePaymentApi.Verify(
                api => api.SingleIdGetAsync(paymentId, It.IsAny<int>(), cancellationSource.Token),
                Times.Once);
        }

        /// <summary>
        /// Provider lookup failures are converted into a stable rejection reason.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsProviderLookupFailure()
        {
            (Donation _, string transactionKey) = await this.SeedPendingDonationAsync();
            Guid paymentId = Guid.NewGuid();
            this.singlePaymentApi
                .Setup(api => api.SingleIdGetAsync(paymentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(503, "Provider unavailable"));

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                CreateNotification(transactionKey, paymentId));

            Assert.False(result.IsValid);
            Assert.Equal("easypay_api_lookup_failed", result.FailureReason);
        }

        /// <summary>
        /// Provider payments that are not paid are rejected.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsUnpaidProviderPayment()
        {
            (Donation donation, string transactionKey) = await this.SeedPendingDonationAsync();
            Guid paymentId = Guid.NewGuid();
            this.ConfigureProviderPayment(paymentId, CreateProviderPayment(donation.PublicId, DonationAmount, "pending"));

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                CreateNotification(transactionKey, paymentId));

            Assert.False(result.IsValid);
            Assert.Equal("easypay_payment_not_paid", result.FailureReason);
        }

        /// <summary>
        /// A provider merchant key for a different donation is rejected.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsMerchantKeyMismatch()
        {
            (Donation _, string transactionKey) = await this.SeedPendingDonationAsync();
            Guid paymentId = Guid.NewGuid();
            this.ConfigureProviderPayment(paymentId, CreateProviderPayment(Guid.NewGuid(), DonationAmount, "paid"));

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                CreateNotification(transactionKey, paymentId));

            Assert.False(result.IsValid);
            Assert.Equal("merchant_key_mismatch", result.FailureReason);
        }

        /// <summary>
        /// Notification amounts that differ from the donation are rejected.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsNotificationAmountMismatch()
        {
            (Donation donation, string transactionKey) = await this.SeedPendingDonationAsync();
            Guid paymentId = Guid.NewGuid();
            this.ConfigureProviderPayment(paymentId, CreateProviderPayment(donation.PublicId, DonationAmount, "paid"));

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                CreateNotification(transactionKey, paymentId, requested: DonationAmount + 1, paid: DonationAmount + 1));

            Assert.False(result.IsValid);
            Assert.Equal("amount_mismatch", result.FailureReason);
        }

        /// <summary>
        /// Provider amounts that differ from the donation are rejected.
        /// </summary>
        [Fact]
        public async Task VerifyTransactionNotificationAsync_RejectsProviderAmountMismatch()
        {
            (Donation donation, string transactionKey) = await this.SeedPendingDonationAsync();
            Guid paymentId = Guid.NewGuid();
            this.ConfigureProviderPayment(paymentId, CreateProviderPayment(donation.PublicId, DonationAmount + 1, "paid"));

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyTransactionNotificationAsync(
                CreateNotification(transactionKey, paymentId));

            Assert.False(result.IsValid);
            Assert.Equal("easypay_value_mismatch", result.FailureReason);
        }

        /// <summary>
        /// Active subscription-create notifications with matching provider data are accepted.
        /// </summary>
        [Fact]
        public async Task VerifyGenericNotificationAsync_AcceptsMatchingActiveSubscription()
        {
            (Guid subscriptionId, string transactionKey) = await this.SeedSubscriptionAsync();
            this.subscriptionPaymentApi
                .Setup(api => api.SubscriptionIdGetAsync(
                    subscriptionId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateProviderSubscription(subscriptionId, transactionKey));
            var notification = new NotificationGeneric(
                id: subscriptionId,
                key: transactionKey,
                type: NotificationGeneric.TypeEnum.SubscriptionCreate,
                status: NotificationGeneric.StatusEnum.Success);

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyGenericNotificationAsync(notification);

            Assert.True(result.IsValid);
            Assert.Null(result.FailureReason);
        }

        /// <summary>
        /// A provider subscription key for another local subscription is rejected.
        /// </summary>
        [Fact]
        public async Task VerifyGenericNotificationAsync_RejectsSubscriptionKeyMismatch()
        {
            (Guid subscriptionId, string transactionKey) = await this.SeedSubscriptionAsync();
            this.subscriptionPaymentApi
                .Setup(api => api.SubscriptionIdGetAsync(
                    subscriptionId,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateProviderSubscription(subscriptionId, "different-key"));
            var notification = new NotificationGeneric(
                id: subscriptionId,
                key: transactionKey,
                type: NotificationGeneric.TypeEnum.SubscriptionCreate,
                status: NotificationGeneric.StatusEnum.Success);

            EasyPayWebhookVerificationResult result = await this.verifier.VerifyGenericNotificationAsync(notification);

            Assert.False(result.IsValid);
            Assert.Equal("subscription_key_mismatch", result.FailureReason);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.dbContext.Dispose();
            this.telemetryConfiguration.Dispose();
        }

        private static TransactionNotificationRequest CreateNotification(
            string transactionKey,
            Guid paymentId,
            double requested = DonationAmount,
            double paid = DonationAmount)
        {
            return new TransactionNotificationRequest(
                id: paymentId,
                transaction: new TransactionNotificationRequestTransaction(
                    key: transactionKey,
                    values: new PaymentSingleTransactionValues(requested: requested, paid: paid)));
        }

        private static InlineObject9 CreateProviderPayment(Guid publicId, double amount, string status)
        {
            return new InlineObject9(
                key: publicId.ToString(),
                value: amount,
                method: new Method(type: "cc", status: status));
        }

        private static SubscriptionIdGet200Response CreateProviderSubscription(Guid subscriptionId, string transactionKey)
        {
            return new SubscriptionIdGet200Response(
                id: subscriptionId,
                key: transactionKey,
                method: new SubscriptionIdGet200ResponseMethod(
                    type: "cc",
                    status: SubscriptionIdGet200ResponseMethod.StatusEnum.Active));
        }

        private void ConfigureProviderPayment(Guid paymentId, InlineObject9 payment)
        {
            this.singlePaymentApi
                .Setup(api => api.SingleIdGetAsync(paymentId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);
        }

        private async Task<(Donation Donation, string TransactionKey)> SeedPendingDonationAsync()
        {
            string transactionKey = Guid.NewGuid().ToString("N");
            var donation = new Donation
            {
                DonationAmount = DonationAmount,
                DonationDate = DateTime.UtcNow,
                PaymentList = new List<BasePayment>
                {
                    new CreditCardPayment
                    {
                        Created = DateTime.UtcNow,
                        Requested = (float)DonationAmount,
                        Status = "pending",
                        TransactionKey = transactionKey,
                    },
                },
                PaymentStatus = PaymentStatus.WaitingPayment,
                PublicId = Guid.NewGuid(),
            };
            this.dbContext.Donations.Add(donation);
            await this.dbContext.SaveChangesAsync();
            return (donation, transactionKey);
        }

        private async Task<(Guid SubscriptionId, string TransactionKey)> SeedSubscriptionAsync()
        {
            Guid subscriptionId = Guid.NewGuid();
            string transactionKey = Guid.NewGuid().ToString("N");
            var donation = new Donation
            {
                DonationAmount = DonationAmount,
                DonationDate = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.WaitingPayment,
                PublicId = Guid.NewGuid(),
            };
            this.dbContext.Subscriptions.Add(new Subscription
            {
                Created = DateTime.UtcNow,
                EasyPaySubscriptionId = subscriptionId.ToString(),
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                Frequency = "1M",
                InitialDonation = donation,
                PublicId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                Status = SubscriptionStatus.Created,
                TransactionKey = transactionKey,
            });
            await this.dbContext.SaveChangesAsync();
            return (subscriptionId, transactionKey);
        }
    }
}
