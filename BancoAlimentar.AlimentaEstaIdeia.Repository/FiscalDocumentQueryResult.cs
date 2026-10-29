// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentQueryResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Represents a safe fiscal-document consultation summary without donor personal data.
    /// </summary>
    public class FiscalDocumentQueryResult
    {
        /// <summary>Gets or sets the fiscal document identifier.</summary>
        public int FiscalDocumentId { get; set; }

        /// <summary>Gets or sets the issuer profile identifier.</summary>
        public int FiscalIssuerProfileId { get; set; }

        /// <summary>Gets or sets the fiscal document series identifier.</summary>
        public int FiscalDocumentSeriesId { get; set; }

        /// <summary>Gets or sets the fiscal document number.</summary>
        public string DocumentNumber { get; set; }

        /// <summary>Gets or sets the fiscal ATCUD.</summary>
        public string Atcud { get; set; }

        /// <summary>Gets or sets the donation identifier.</summary>
        public int DonationId { get; set; }

        /// <summary>Gets or sets the fiscal invoice date.</summary>
        public DateOnly InvoiceDate { get; set; }

        /// <summary>Gets or sets the fiscal document status.</summary>
        public FiscalDocumentStatus Status { get; set; }
    }
}
