// -----------------------------------------------------------------------
// <copyright file="FiscalQrPayloadSnapshot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;

    /// <summary>
    /// Contains final, persisted fiscal values required by the Portuguese QR specification.
    /// </summary>
    public sealed class FiscalQrPayloadSnapshot
    {
        /// <summary>Gets or sets the issuer's tax registration number.</summary>
        public string IssuerTaxRegistrationNumber { get; init; }

        /// <summary>Gets or sets the buyer's tax registration number.</summary>
        public string BuyerTaxRegistrationNumber { get; init; }

        /// <summary>Gets or sets the buyer country code.</summary>
        public string BuyerCountryCode { get; init; }

        /// <summary>Gets or sets the fiscal document type.</summary>
        public string DocumentType { get; init; }

        /// <summary>Gets or sets the fiscal document status.</summary>
        public string DocumentStatus { get; init; }

        /// <summary>Gets or sets the issued invoice date.</summary>
        public DateOnly InvoiceDate { get; init; }

        /// <summary>Gets or sets the invoice identifier.</summary>
        public string InvoiceIdentifier { get; init; }

        /// <summary>Gets or sets the ATCUD.</summary>
        public string Atcud { get; init; }

        /// <summary>Gets or sets the tax country or region.</summary>
        public string TaxCountryRegion { get; init; }

        /// <summary>Gets or sets the taxable base at the normal rate.</summary>
        public decimal TaxableBaseNormal { get; init; }

        /// <summary>Gets or sets the tax amount at the normal rate.</summary>
        public decimal TaxAmountNormal { get; init; }

        /// <summary>Gets or sets the payable tax total.</summary>
        public decimal TaxPayable { get; init; }

        /// <summary>Gets or sets the gross document total.</summary>
        public decimal GrossTotal { get; init; }

        /// <summary>Gets or sets the document signature hash.</summary>
        public string Hash { get; init; }

        /// <summary>Gets or sets the signature hash control.</summary>
        public string HashControl { get; init; }

        /// <summary>Gets or sets the AT-certified software certificate number.</summary>
        public string SoftwareCertificateNumber { get; init; }
    }
}
