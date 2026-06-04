// -----------------------------------------------------------------------
// <copyright file="IntegrationTestEasyPayWebhookVerifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// In-memory webhook verifier for integration tests (no external Easypay API calls).
    /// </summary>
    public class IntegrationTestEasyPayWebhookVerifier : IEasyPayWebhookVerifier
    {
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTestEasyPayWebhookVerifier"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public IntegrationTestEasyPayWebhookVerifier(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <inheritdoc />
        public Task<EasyPayWebhookVerificationResult> VerifyTransactionNotificationAsync(
            TransactionNotificationRequest notification,
            CancellationToken cancellationToken = default)
        {
            if (notification?.Transaction == null || string.IsNullOrWhiteSpace(notification.Transaction.Key))
            {
                return Task.FromResult(EasyPayWebhookVerificationResult.Invalid("missing_transaction"));
            }

            return this.VerifyPaymentNotificationAsync(
                notification.Transaction.Key,
                notification.Transaction.Values?.Requested ?? 0,
                notification.Transaction.Values?.Paid ?? 0,
                cancellationToken);
        }

        /// <inheritdoc />
        public Task<EasyPayWebhookVerificationResult> VerifyGenericNotificationAsync(
            NotificationGeneric notification,
            CancellationToken cancellationToken = default)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.Key))
            {
                return Task.FromResult(EasyPayWebhookVerificationResult.Invalid("missing_notification"));
            }

            if (notification.Type == NotificationGeneric.TypeEnum.SubscriptionCreate
                || notification.Type == NotificationGeneric.TypeEnum.SubscriptionCapture)
            {
                return this.VerifySubscriptionNotificationAsync(notification.Key, cancellationToken);
            }

            if (notification.Type != NotificationGeneric.TypeEnum.Capture
                && notification.Type != NotificationGeneric.TypeEnum.Authorisation)
            {
                return Task.FromResult(EasyPayWebhookVerificationResult.Valid());
            }

            if (notification.Status != NotificationGeneric.StatusEnum.Success)
            {
                return Task.FromResult(EasyPayWebhookVerificationResult.Valid());
            }

            return this.VerifyPaymentNotificationAsync(notification.Key, null, null, cancellationToken);
        }

        private async Task<EasyPayWebhookVerificationResult> VerifyPaymentNotificationAsync(
            string transactionKey,
            double? requested,
            double? paid,
            CancellationToken cancellationToken)
        {
            var donation = await this.FindDonationByTransactionKeyAsync(transactionKey, cancellationToken)
                .ConfigureAwait(false);

            if (donation == null)
            {
                return EasyPayWebhookVerificationResult.Invalid("unknown_transaction_key");
            }

            if (requested.HasValue && paid.HasValue
                && !PaymentAmountReconciliation.AmountsMatchDonation(donation.DonationAmount, requested.Value, paid.Value))
            {
                return EasyPayWebhookVerificationResult.Invalid("amount_mismatch");
            }

            return EasyPayWebhookVerificationResult.Valid();
        }

        private async Task<EasyPayWebhookVerificationResult> VerifySubscriptionNotificationAsync(
            string transactionKey,
            CancellationToken cancellationToken)
        {
            var exists = await this.dbContext.Subscriptions
                .AsNoTracking()
                .AnyAsync(s => s.TransactionKey == transactionKey, cancellationToken)
                .ConfigureAwait(false);

            return exists
                ? EasyPayWebhookVerificationResult.Valid()
                : EasyPayWebhookVerificationResult.Invalid("unknown_subscription_key");
        }

        private async Task<Donation> FindDonationByTransactionKeyAsync(string transactionKey, CancellationToken cancellationToken)
        {
            var payment = await this.dbContext.Payments
                .AsNoTracking()
                .Include(p => p.Donation)
                .Where(p => p.TransactionKey == transactionKey)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (payment?.Donation != null)
            {
                return payment.Donation;
            }

            return await this.dbContext.Donations
                .AsNoTracking()
                .Where(d => d.PaymentList.Any(p => p.TransactionKey == transactionKey))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
