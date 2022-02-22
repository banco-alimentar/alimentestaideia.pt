// -----------------------------------------------------------------------
// <copyright file="EasyPayBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Microsoft.AspNetCore.Http;
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
        /// <param name="httpContext">Http Context accessor.</param>
        public EasyPayBuilder(IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            easypayConfig = new Configuration
            {
                BasePath = configuration["Easypay:BaseUrl"] + "/2.0",
            };
            easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
            easypayConfig.UserAgent = $" {GetType().Assembly.GetName().Name}/{GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Configuration.Version})";

            string accountId = null;
            string apiKey = null;

            Tenant tenant = httpContext.HttpContext.GetTenant();
            if (tenant.PaymentStrategy == Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor)
            {
                accountId = configuration["Easypay:AccountId"];
                apiKey = configuration["Easypay:ApiKey"];
            }
            else if (tenant.PaymentStrategy == Sas.Model.Strategy.PaymentStrategy.IndividualPaymentProcessorPerFoodBank)
            {
                int? foodBankId = httpContext.HttpContext.Session.GetFoodBankId();
                if (foodBankId.HasValue)
                {
                    accountId = configuration[$"Easypay:AccountId-{foodBankId.Value}"];
                    apiKey = configuration[$"Easypay:ApiKey-{foodBankId.Value}"];
                }
                else
                {
                    throw new InvalidOperationException($"Tenant payment strategy is {tenant.PaymentStrategy} and we can't found a valid Food Bank id in session.");
                }
            }

            easypayConfig.ApiKey.Add("AccountId", accountId);
            easypayConfig.ApiKey.Add("ApiKey", apiKey);
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
