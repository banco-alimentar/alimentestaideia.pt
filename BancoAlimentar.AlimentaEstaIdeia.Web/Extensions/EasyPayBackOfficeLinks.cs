// -----------------------------------------------------------------------
// <copyright file="EasyPayBackOfficeLinks.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System;

    /// <summary>
    /// Builds links to Easypay back-office records.
    /// </summary>
    public static class EasyPayBackOfficeLinks
    {
        private const string BackOfficeUrl = "https://backoffice.easypay.pt/payments/v2/single";
        private const string AccountId = "338300db-31e0-4ed0-bc63-0881d0befad3";

        /// <summary>
        /// Builds the Easypay back-office link for a single payment.
        /// </summary>
        /// <param name="paymentId">The Easypay transaction ID.</param>
        /// <returns>The back-office URL, or <see langword="null"/> when no payment ID is available.</returns>
        public static string BuildPaymentUrl(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return null;
            }

            return $"{BackOfficeUrl}/{AccountId}/{Uri.EscapeDataString(paymentId)}";
        }
    }
}
