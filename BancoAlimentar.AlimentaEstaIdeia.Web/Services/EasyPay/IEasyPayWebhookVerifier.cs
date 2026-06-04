// -----------------------------------------------------------------------
// <copyright file="IEasyPayWebhookVerifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Verifies Easypay webhook authenticity before payment state changes.
    /// </summary>
    public interface IEasyPayWebhookVerifier
    {
        /// <summary>
        /// Verifies a transaction (capture) notification.
        /// </summary>
        /// <param name="notification">Webhook payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Verification outcome.</returns>
        Task<EasyPayWebhookVerificationResult> VerifyTransactionNotificationAsync(
            TransactionNotificationRequest notification,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a generic notification.
        /// </summary>
        /// <param name="notification">Webhook payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Verification outcome.</returns>
        Task<EasyPayWebhookVerificationResult> VerifyGenericNotificationAsync(
            NotificationGeneric notification,
            CancellationToken cancellationToken = default);
    }
}
