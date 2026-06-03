// -----------------------------------------------------------------------
// <copyright file="EasyPayApiCredentialsFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Easypay.Rest.Client.Client;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Builds Easypay REST clients for webhook verification (supports per-food-bank credentials).
    /// </summary>
    public class EasyPayApiCredentialsFactory
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayApiCredentialsFactory"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public EasyPayApiCredentialsFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Creates an Easypay client configuration for the given tenant and optional food bank.
        /// </summary>
        /// <param name="tenant">Current tenant.</param>
        /// <param name="foodBankId">Food bank id when using individual processors.</param>
        /// <returns>Easypay REST configuration.</returns>
        public Configuration CreateConfiguration(Tenant tenant, int? foodBankId)
        {
            var easypayConfig = new Configuration
            {
                BasePath = this.configuration["Easypay:BaseUrl"] + "/2.0",
            };
            easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");

            string accountId = null;
            string apiKey = null;

            if (tenant.PaymentStrategy == PaymentStrategy.SharedPaymentProcessor)
            {
                accountId = this.configuration["Easypay:AccountId"];
                apiKey = this.configuration["Easypay:ApiKey"];
            }
            else if (tenant.PaymentStrategy == PaymentStrategy.IndividualPaymentProcessorPerFoodBank)
            {
                if (!foodBankId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Food bank id is required to verify Easypay webhooks for IndividualPaymentProcessorPerFoodBank.");
                }

                accountId = this.configuration[$"Easypay:AccountId-{foodBankId.Value}"];
                apiKey = this.configuration[$"Easypay:ApiKey-{foodBankId.Value}"];
            }

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Easypay AccountId or ApiKey is not configured for webhook verification.");
            }

            easypayConfig.ApiKey.Add("AccountId", accountId);
            easypayConfig.ApiKey.Add("ApiKey", apiKey);
            return easypayConfig;
        }
    }
}
