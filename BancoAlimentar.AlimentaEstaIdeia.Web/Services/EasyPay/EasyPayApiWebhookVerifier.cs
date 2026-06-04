// -----------------------------------------------------------------------
// <copyright file="EasyPayApiWebhookVerifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Verifies Easypay webhooks by querying the Easypay API (recommended by Easypay documentation).
    /// </summary>
    public class EasyPayApiWebhookVerifier : IEasyPayWebhookVerifier
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly EasyPayApiCredentialsFactory credentialsFactory;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayApiWebhookVerifier"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        /// <param name="credentialsFactory">Easypay credentials factory.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        public EasyPayApiWebhookVerifier(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            EasyPayApiCredentialsFactory credentialsFactory,
            TelemetryClient telemetryClient)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.credentialsFactory = credentialsFactory;
            this.telemetryClient = telemetryClient;
        }

        /// <inheritdoc />
        public async Task<EasyPayWebhookVerificationResult> VerifyTransactionNotificationAsync(
            TransactionNotificationRequest notification,
            CancellationToken cancellationToken = default)
        {
            if (notification?.Transaction == null || string.IsNullOrWhiteSpace(notification.Transaction.Key))
            {
                return EasyPayWebhookVerificationResult.Invalid("missing_transaction");
            }

            var paymentContext = await this.ResolvePaymentContextAsync(notification.Transaction.Key, cancellationToken);
            if (paymentContext.Donation == null)
            {
                return EasyPayWebhookVerificationResult.Invalid("unknown_transaction_key");
            }

            if (notification.Id == default)
            {
                return EasyPayWebhookVerificationResult.Invalid("missing_payment_id");
            }

            Easypay.Rest.Client.Model.Single verifiedPayment;
            try
            {
                var tenant = this.httpContextAccessor.HttpContext.GetTenant();
                var config = this.credentialsFactory.CreateConfiguration(tenant, paymentContext.FoodBankId);
                var api = new SinglePaymentApi(config);
                verifiedPayment = await api.SingleIdGetAsync(notification.Id, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ApiException ex)
            {
                this.TrackVerificationFailure("EasypayApiLookupFailed", notification.Transaction.Key, ex.Message);
                return EasyPayWebhookVerificationResult.Invalid("easypay_api_lookup_failed");
            }

            if (verifiedPayment == null)
            {
                return EasyPayWebhookVerificationResult.Invalid("easypay_payment_not_found");
            }

            if (verifiedPayment.PaymentStatus != SinglePaymentStatus.Paid)
            {
                return EasyPayWebhookVerificationResult.Invalid("easypay_payment_not_paid");
            }

            if (!string.IsNullOrWhiteSpace(verifiedPayment.Key)
                && !string.Equals(verifiedPayment.Key, paymentContext.Donation.PublicId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return EasyPayWebhookVerificationResult.Invalid("merchant_key_mismatch");
            }

            double requested = notification.Transaction.Values?.Requested ?? 0;
            double paid = notification.Transaction.Values?.Paid ?? 0;
            if (!PaymentAmountReconciliation.AmountsMatchDonation(
                paymentContext.Donation.DonationAmount,
                requested,
                paid))
            {
                this.TrackVerificationFailure(
                    "WebhookAmountMismatch",
                    notification.Transaction.Key,
                    $"expected={paymentContext.Donation.DonationAmount}, requested={requested}, paid={paid}");
                return EasyPayWebhookVerificationResult.Invalid("amount_mismatch");
            }

            if (!PaymentAmountReconciliation.ProviderValueMatchesDonation(
                paymentContext.Donation.DonationAmount,
                verifiedPayment.Value))
            {
                return EasyPayWebhookVerificationResult.Invalid("easypay_value_mismatch");
            }

            return EasyPayWebhookVerificationResult.Valid();
        }

        /// <inheritdoc />
        public async Task<EasyPayWebhookVerificationResult> VerifyGenericNotificationAsync(
            NotificationGeneric notification,
            CancellationToken cancellationToken = default)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.Key))
            {
                return EasyPayWebhookVerificationResult.Invalid("missing_notification");
            }

            if (notification.Type == NotificationGeneric.TypeEnum.SubscriptionCreate
                || notification.Type == NotificationGeneric.TypeEnum.SubscriptionCapture)
            {
                return await this.VerifySubscriptionGenericNotificationAsync(notification, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (notification.Type != NotificationGeneric.TypeEnum.Capture
                && notification.Type != NotificationGeneric.TypeEnum.Authorisation)
            {
                return EasyPayWebhookVerificationResult.Valid();
            }

            if (notification.Status != NotificationGeneric.StatusEnum.Success)
            {
                return EasyPayWebhookVerificationResult.Valid();
            }

            var paymentContext = await this.ResolvePaymentContextAsync(notification.Key, cancellationToken);
            if (paymentContext.Donation == null)
            {
                return EasyPayWebhookVerificationResult.Invalid("unknown_transaction_key");
            }

            if (notification.Id == default)
            {
                return EasyPayWebhookVerificationResult.Invalid("missing_payment_id");
            }

            Easypay.Rest.Client.Model.Single verifiedPayment;
            try
            {
                var tenant = this.httpContextAccessor.HttpContext.GetTenant();
                var config = this.credentialsFactory.CreateConfiguration(tenant, paymentContext.FoodBankId);
                var api = new SinglePaymentApi(config);
                verifiedPayment = await api.SingleIdGetAsync(notification.Id, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ApiException ex)
            {
                this.TrackVerificationFailure("EasypayApiLookupFailed", notification.Key, ex.Message);
                return EasyPayWebhookVerificationResult.Invalid("easypay_api_lookup_failed");
            }

            if (verifiedPayment == null)
            {
                return EasyPayWebhookVerificationResult.Invalid("easypay_payment_not_found");
            }

            if (verifiedPayment.PaymentStatus != SinglePaymentStatus.Paid)
            {
                return EasyPayWebhookVerificationResult.Invalid("easypay_payment_not_paid");
            }

            if (!string.IsNullOrWhiteSpace(verifiedPayment.Key)
                && !string.Equals(verifiedPayment.Key, paymentContext.Donation.PublicId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return EasyPayWebhookVerificationResult.Invalid("merchant_key_mismatch");
            }

            if (!PaymentAmountReconciliation.ProviderValueMatchesDonation(
                paymentContext.Donation.DonationAmount,
                verifiedPayment.Value))
            {
                return EasyPayWebhookVerificationResult.Invalid("easypay_value_mismatch");
            }

            return EasyPayWebhookVerificationResult.Valid();
        }

        private async Task<EasyPayWebhookVerificationResult> VerifySubscriptionGenericNotificationAsync(
            NotificationGeneric notification,
            CancellationToken cancellationToken)
        {
            var subscription = await this.dbContext.Subscriptions
                .AsNoTracking()
                .Include(s => s.InitialDonation)
                .ThenInclude(d => d.FoodBank)
                .Where(s => s.TransactionKey == notification.Key)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (subscription == null)
            {
                return EasyPayWebhookVerificationResult.Invalid("unknown_subscription_key");
            }

            if (string.IsNullOrWhiteSpace(subscription.EasyPaySubscriptionId)
                || !Guid.TryParse(subscription.EasyPaySubscriptionId, out Guid easyPaySubscriptionId))
            {
                return EasyPayWebhookVerificationResult.Invalid("missing_easypay_subscription_id");
            }

            if (notification.Id != default && notification.Id != easyPaySubscriptionId)
            {
                return EasyPayWebhookVerificationResult.Invalid("subscription_id_mismatch");
            }

            try
            {
                var tenant = this.httpContextAccessor.HttpContext.GetTenant();
                int? foodBankId = subscription.InitialDonation?.FoodBank?.Id;
                var config = this.credentialsFactory.CreateConfiguration(tenant, foodBankId);
                var api = new SubscriptionPaymentApi(config);
                var verified = await api.SubscriptionIdGetAsync(easyPaySubscriptionId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (verified == null)
                {
                    return EasyPayWebhookVerificationResult.Invalid("easypay_subscription_not_found");
                }

                if (!string.Equals(verified.Key, notification.Key, StringComparison.Ordinal))
                {
                    return EasyPayWebhookVerificationResult.Invalid("subscription_key_mismatch");
                }

                if (notification.Type == NotificationGeneric.TypeEnum.SubscriptionCreate
                    && notification.Status == NotificationGeneric.StatusEnum.Success
                    && verified.Method?.Status != SubscriptionIdGet200ResponseMethod.StatusEnum.Active)
                {
                    return EasyPayWebhookVerificationResult.Invalid("subscription_not_active");
                }
            }
            catch (ApiException ex)
            {
                this.TrackVerificationFailure("EasypaySubscriptionLookupFailed", notification.Key, ex.Message);
                return EasyPayWebhookVerificationResult.Invalid("easypay_subscription_lookup_failed");
            }

            return EasyPayWebhookVerificationResult.Valid();
        }

        private async Task<(Donation Donation, int? FoodBankId)> ResolvePaymentContextAsync(
            string transactionKey,
            CancellationToken cancellationToken)
        {
            var payment = await this.dbContext.Payments
                .AsNoTracking()
                .Include(p => p.Donation)
                .ThenInclude(d => d.FoodBank)
                .Where(p => p.TransactionKey == transactionKey)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (payment?.Donation != null)
            {
                return (payment.Donation, payment.Donation.FoodBank?.Id);
            }

            var donation = await this.dbContext.Donations
                .AsNoTracking()
                .Include(d => d.FoodBank)
                .Where(d => d.PaymentList.Any(p => p.TransactionKey == transactionKey))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return (donation, donation?.FoodBank?.Id);
        }

        private void TrackVerificationFailure(string eventName, string transactionKey, string detail)
        {
            this.telemetryClient.TrackEvent(
                eventName,
                new Dictionary<string, string>
                {
                    { "TransactionKey", transactionKey },
                    { "Detail", detail },
                });
        }
    }
}
