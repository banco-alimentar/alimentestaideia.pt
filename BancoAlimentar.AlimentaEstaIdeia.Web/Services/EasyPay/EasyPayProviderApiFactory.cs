// -----------------------------------------------------------------------
// <copyright file="EasyPayProviderApiFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;

    /// <summary>
    /// Creates Easypay API clients from tenant-specific credentials.
    /// </summary>
    public interface IEasyPayProviderApiFactory
    {
        /// <summary>
        /// Creates a single-payment API client.
        /// </summary>
        /// <param name="configuration">Easypay client configuration.</param>
        /// <returns>A single-payment API client.</returns>
        ISinglePaymentApi CreateSinglePaymentApi(Configuration configuration);

        /// <summary>
        /// Creates a subscription-payment API client.
        /// </summary>
        /// <param name="configuration">Easypay client configuration.</param>
        /// <returns>A subscription-payment API client.</returns>
        ISubscriptionPaymentApi CreateSubscriptionPaymentApi(Configuration configuration);
    }

    /// <summary>
    /// Default factory for generated Easypay API clients.
    /// </summary>
    public sealed class EasyPayProviderApiFactory : IEasyPayProviderApiFactory
    {
        /// <inheritdoc />
        public ISinglePaymentApi CreateSinglePaymentApi(Configuration configuration)
        {
            return new SinglePaymentApi(configuration);
        }

        /// <inheritdoc />
        public ISubscriptionPaymentApi CreateSubscriptionPaymentApi(Configuration configuration)
        {
            return new SubscriptionPaymentApi(configuration);
        }
    }
}
