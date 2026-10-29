// -----------------------------------------------------------------------
// <copyright file="FiscalQrPayloadBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Builds Portuguese fiscal QR payloads according to the AT technical specification.
    /// </summary>
    public class FiscalQrPayloadBuilder : IFiscalQrPayloadBuilder
    {
        /// <inheritdoc />
        public string Build(FiscalQrPayloadSnapshot snapshot)
        {
            ValidateSnapshot(snapshot);

            return string.Join(
                "*",
                new[]
                {
                    $"A:{snapshot.IssuerTaxRegistrationNumber}",
                    $"B:{snapshot.BuyerTaxRegistrationNumber}",
                    $"C:{snapshot.BuyerCountryCode}",
                    $"D:{snapshot.DocumentType}",
                    $"E:{snapshot.DocumentStatus}",
                    $"F:{snapshot.InvoiceDate:yyyyMMdd}",
                    $"G:{snapshot.InvoiceIdentifier}",
                    $"H:{snapshot.Atcud}",
                    $"I1:{snapshot.TaxCountryRegion}",
                    $"I7:{FormatAmount(snapshot.TaxableBaseNormal)}",
                    $"I8:{FormatAmount(snapshot.TaxAmountNormal)}",
                    $"N:{FormatAmount(snapshot.TaxPayable)}",
                    $"O:{FormatAmount(snapshot.GrossTotal)}",
                    $"Q:{GetHashExcerpt(snapshot.Hash)}",
                    $"R:{snapshot.SoftwareCertificateNumber}",
                });
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string GetHashExcerpt(string hash)
        {
            return string.Concat(hash[0], hash[10], hash[20], hash[30]);
        }

        private static void ValidateSnapshot(FiscalQrPayloadSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new InvalidOperationException("QR payload requires a final fiscal snapshot.");
            }

            ValidateRequiredValue(snapshot.IssuerTaxRegistrationNumber, nameof(snapshot.IssuerTaxRegistrationNumber));
            ValidateRequiredValue(snapshot.BuyerTaxRegistrationNumber, nameof(snapshot.BuyerTaxRegistrationNumber));
            ValidateRequiredValue(snapshot.BuyerCountryCode, nameof(snapshot.BuyerCountryCode));
            ValidateRequiredValue(snapshot.DocumentType, nameof(snapshot.DocumentType));
            ValidateRequiredValue(snapshot.DocumentStatus, nameof(snapshot.DocumentStatus));
            ValidateRequiredValue(snapshot.InvoiceIdentifier, nameof(snapshot.InvoiceIdentifier));
            ValidateRequiredValue(snapshot.Atcud, nameof(snapshot.Atcud));
            ValidateRequiredValue(snapshot.TaxCountryRegion, nameof(snapshot.TaxCountryRegion));
            ValidateRequiredValue(snapshot.Hash, nameof(snapshot.Hash));
            ValidateRequiredValue(snapshot.HashControl, nameof(snapshot.HashControl));
            ValidateRequiredValue(snapshot.SoftwareCertificateNumber, nameof(snapshot.SoftwareCertificateNumber));

            if (snapshot.InvoiceDate == DateOnly.MinValue)
            {
                throw new InvalidOperationException("QR payload requires an invoice date.");
            }

            if (snapshot.Hash.Length < 31)
            {
                throw new InvalidOperationException("QR payload requires a complete fiscal hash.");
            }
        }

        private static void ValidateRequiredValue(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"QR payload requires {fieldName}.");
            }

            if (value.Contains("*", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"QR payload has an invalid {fieldName}.");
            }
        }
    }
}
