// -----------------------------------------------------------------------
// <copyright file="EasyPayBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
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
        private ISubscriptionPaymentApi subscriptionPaymentApiOverride;
        private ISinglePaymentApi singlePaymentApiOverride;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayBuilder"/> class.
        /// </summary>
        /// <param name="configuration">A reference to the <see cref="IConfiguration"/> class.</param>
        /// <param name="httpContext">Http Context accessor.</param>
        public EasyPayBuilder(IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            this.easypayConfig = new Configuration
            {
                BasePath = configuration["Easypay:BaseUrl"] + "/2.0",
            };
            this.easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
            this.easypayConfig.UserAgent = $" {this.GetType().Assembly.GetName().Name}/{this.GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Configuration.Version})";

            string accountId = null;
            string apiKey = null;

            Tenant tenant = httpContext.HttpContext.GetTenant();
            if (tenant.PaymentStrategy == PaymentStrategy.SharedPaymentProcessor)
            {
                accountId = configuration["Easypay:AccountId"];
                apiKey = configuration["Easypay:ApiKey"];
            }
            else if (tenant.PaymentStrategy == PaymentStrategy.IndividualPaymentProcessorPerFoodBank)
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

            this.easypayConfig.ApiKey.Add("AccountId", accountId);
            this.easypayConfig.ApiKey.Add("ApiKey", apiKey);
        }

        /// <summary>
        /// Gets the <see cref="SinglePaymentApi"/>.
        /// </summary>
        /// <returns>A reference to the <see cref="ISinglePaymentApi"/>.</returns>
        public ISinglePaymentApi GetSinglePaymentApi()
        {
            if (this.singlePaymentApiOverride != null)
            {
                return this.singlePaymentApiOverride;
            }

            return new SinglePaymentApi(this.easypayConfig);
        }

        /// <summary>
        /// Replaces the single-payment API client (integration tests only).
        /// </summary>
        /// <param name="api">Stub or mock implementation.</param>
        public void SetSinglePaymentApiOverride(ISinglePaymentApi api)
        {
            this.singlePaymentApiOverride = api;
        }

        /// <summary>
        /// Replaces the subscription API client (integration tests only).
        /// </summary>
        /// <param name="api">Stub or mock implementation.</param>
        public void SetSubscriptionPaymentApiOverride(ISubscriptionPaymentApi api)
        {
            this.subscriptionPaymentApiOverride = api;
        }

        /// <summary>
        /// Gets the subscription payment API client.
        /// </summary>
        /// <returns>A reference to the <see cref="ISubscriptionPaymentApi"/>.</returns>
        public ISubscriptionPaymentApi GetSubscriptionPaymentApi()
        {
            if (this.subscriptionPaymentApiOverride != null)
            {
                return this.subscriptionPaymentApiOverride;
            }

            return new SubscriptionPaymentApi(this.easypayConfig);
        }
    }
}
