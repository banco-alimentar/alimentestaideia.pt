// -----------------------------------------------------------------------
// <copyright file="EasyPayBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This service help build the EasyPay API and its configuration.
    /// </summary>
    public class EasyPayBuilder
    {
        private readonly Configuration easypayConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayBuilder"/> class.
        /// </summary>
        /// <param name="configuration">A reference to the <see cref="IConfiguration"/> class.</param>
        public EasyPayBuilder(IConfiguration configuration)
        {
            easypayConfig = new Configuration();
            easypayConfig.BasePath = configuration["Easypay:BaseUrl"] + "/2.0";
            easypayConfig.ApiKey.Add("AccountId", configuration["Easypay:AccountId"]);
            easypayConfig.ApiKey.Add("ApiKey", configuration["Easypay:ApiKey"]);
            easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
            easypayConfig.UserAgent = $" {GetType().Assembly.GetName().Name}/{GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Configuration.Version})";
        }

        /// <summary>
        /// Gets the <see cref="SinglePaymentApi"/>.
        /// </summary>
        /// <returns>A reference to the <see cref="SinglePaymentApi"/>.</returns>
        public SinglePaymentApi GetSinglePaymentApi()
        {
            return new SinglePaymentApi(easypayConfig);
        }

        /// <summary>
        /// Gets the <see cref="SubscriptionPaymentApi"/>.
        /// </summary>
        /// <returns>A reference to the <see cref="SubscriptionPaymentApi"/>.</returns>
        public SubscriptionPaymentApi GetSubscriptionPaymentApi()
        {
            return new SubscriptionPaymentApi(easypayConfig);
        }
    }
}
