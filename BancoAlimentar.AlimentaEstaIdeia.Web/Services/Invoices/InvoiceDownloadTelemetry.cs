// -----------------------------------------------------------------------
// <copyright file="InvoiceDownloadTelemetry.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Redacts donation identifiers before they are written to telemetry.
    /// </summary>
    internal static class InvoiceDownloadTelemetry
    {
        /// <summary>
        /// Returns a stable, non-reversible hash prefix for a public donation id.
        /// </summary>
        /// <param name="publicDonationId">Public donation identifier.</param>
        /// <returns>Redacted identifier safe for logs.</returns>
        public static string RedactPublicDonationId(string publicDonationId)
        {
            if (string.IsNullOrWhiteSpace(publicDonationId) || !Guid.TryParse(publicDonationId, out _))
            {
                return "invalid";
            }

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(publicDonationId.ToUpperInvariant()));
            return Convert.ToHexString(hash)[..12];
        }
    }
}
