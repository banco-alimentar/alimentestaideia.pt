// -----------------------------------------------------------------------
// <copyright file="PermissiveEasyPayWebhookVerifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Webhook verifier that accepts all notifications (for integration tests only).
    /// </summary>
    public class PermissiveEasyPayWebhookVerifier : IEasyPayWebhookVerifier
    {
        /// <inheritdoc />
        public Task<EasyPayWebhookVerificationResult> VerifyTransactionNotificationAsync(
            TransactionNotificationRequest notification,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EasyPayWebhookVerificationResult.Valid());
        }

        /// <inheritdoc />
        public Task<EasyPayWebhookVerificationResult> VerifyGenericNotificationAsync(
            NotificationGeneric notification,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(EasyPayWebhookVerificationResult.Valid());
        }
    }
}
