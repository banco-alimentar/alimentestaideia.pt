// -----------------------------------------------------------------------
// <copyright file="EasyPayBackOfficeLinks.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Builds links to Easypay back-office records.
    /// </summary>
    public static class EasyPayBackOfficeLinks
    {
        private const string ProductionBackOfficeUrl = "https://backoffice.easypay.pt";
        private const string ProductionSubscriptionBackOfficeUrl = "https://bo.easypay.pt";
        private const string DevelopmentBackOfficeUrl = "https://backoffice.test.easypay.pt";
        private const string AccountId = "338300db-31e0-4ed0-bc63-0881d0befad3";

        /// <summary>
        /// Builds the Easypay back-office link for a single payment.
        /// </summary>
        /// <param name="paymentId">The Easypay transaction ID.</param>
        /// <param name="environment">The current hosting environment.</param>
        /// <returns>The back-office URL, or <see langword="null"/> when no payment ID is available.</returns>
        public static string BuildPaymentUrl(string paymentId, IHostEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return null;
            }

            return $"{GetBackOfficeUrl(environment)}/payments/v2/single/{AccountId}/{Uri.EscapeDataString(paymentId)}";
        }

        /// <summary>
        /// Builds the Easypay back-office link for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The Easypay subscription ID.</param>
        /// <param name="environment">The current hosting environment.</param>
        /// <returns>The back-office URL, or <see langword="null"/> when no subscription ID is available.</returns>
        public static string BuildSubscriptionUrl(string subscriptionId, IHostEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return null;
            }

            return $"{GetSubscriptionBackOfficeUrl(environment)}/subscription/{Uri.EscapeDataString(subscriptionId)}";
        }

        private static string GetBackOfficeUrl(IHostEnvironment environment)
        {
            return environment?.IsDevelopment() == true
                ? DevelopmentBackOfficeUrl
                : ProductionBackOfficeUrl;
        }

        private static string GetSubscriptionBackOfficeUrl(IHostEnvironment environment)
        {
            return environment?.IsDevelopment() == true
                ? DevelopmentBackOfficeUrl
                : ProductionSubscriptionBackOfficeUrl;
        }
    }
}
