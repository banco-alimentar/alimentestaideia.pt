// -----------------------------------------------------------------------
// <copyright file="PayPalBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using PayPalCheckoutSdk.Core;
    using PayPalCheckoutSdk.Orders;
    using PayPalHttp;

    /// <summary>
    /// This service help build the PayPal API and its configuration.
    /// </summary>
    public class PayPalBuilder
    {
        private readonly HttpClient paypalClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayPalBuilder"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="httpContext">Http Context accessor.</param>
        public PayPalBuilder(IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            string clientId = null;
            string clientSecret = null;
            Tenant tenant = httpContext.HttpContext.GetTenant();
            if (tenant.PaymentStrategy == Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor)
            {
                clientId = configuration["PayPal:clientId"];
                clientSecret = configuration["PayPal:clientSecret"];
            }
            else if (tenant.PaymentStrategy == Sas.Model.Strategy.PaymentStrategy.IndividualPaymentProcessorPerFoodBank)
            {
                int? foodBankId = httpContext.HttpContext.Session.GetFoodBankId();
                if (foodBankId.HasValue)
                {
                    clientId = configuration[$"PayPal:clientId-{foodBankId.Value}"];
                    clientSecret = configuration[$"PayPal:clientSecret-{foodBankId.Value}"];
                }
                else
                {
                    throw new InvalidOperationException($"Tenant payment strategy is {tenant.PaymentStrategy} and we can't found a valid Food Bank id in session.");
                }
            }

            paypalClient = new PayPalHttpClient(PayPalClient.GetPayPalEnvironment(clientId, clientSecret, configuration["PayPal:mode"] == "live"));
        }

        /// <summary>
        /// Gets the PayPal client.
        /// </summary>
        /// <returns>A reference to the <see cref="HttpClient"/> for PayPal.</returns>
        public HttpClient GetPayPalHttpClient()
        {
            return paypalClient;
        }
    }
}
