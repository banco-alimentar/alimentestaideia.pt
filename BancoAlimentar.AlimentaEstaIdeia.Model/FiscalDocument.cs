// -----------------------------------------------------------------------
// <copyright file="FiscalDocument.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Stores the immutable fiscal evidence issued for a donation.
    /// </summary>
    public class FiscalDocument
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the durable key that makes an original-document reservation idempotent.
        /// It is absent from documents not created by the reservation primitive.
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// Gets or sets the donation identifier.
        /// </summary>
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the donation represented by the document.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the fiscal series identifier.
        /// </summary>
        public int SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the fiscal series.
        /// </summary>
        public FiscalDocumentSeries Series { get; set; }

        /// <summary>
        /// Gets or sets the issuing profile identifier.
        /// </summary>
        public int FiscalIssuerProfileId { get; set; }

        /// <summary>
        /// Gets or sets the issuing profile.
        /// </summary>
        public FiscalIssuerProfile FiscalIssuerProfile { get; set; }

        /// <summary>
        /// Gets or sets the invoicing strategy captured at issuance.
        /// </summary>
        public InvoicingStrategy InvoicingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the optional food bank boundary captured at issuance.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the original fiscal document identifier for corrections and credits.
        /// </summary>
        public int? OriginalFiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the original fiscal document for corrections and credits.
        /// </summary>
        public FiscalDocument OriginalFiscalDocument { get; set; }

        /// <summary>
        /// Gets or sets the corrections and credits associated with this document.
        /// </summary>
        public ICollection<FiscalDocument> Corrections { get; set; }

        /// <summary>
        /// Gets or sets the kind of fiscal document.
        /// </summary>
        public FiscalDocumentKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the fiscal document lifecycle state.
        /// </summary>
        public FiscalDocumentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the sequence reserved within the fiscal series.
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the human-readable document number.
        /// </summary>
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Gets or sets the ATCUD derived from the series validation code and sequence.
        /// </summary>
        public string Atcud { get; set; }

        /// <summary>
        /// Gets or sets when the document was issued in UTC.
        /// </summary>
        public DateTimeOffset IssuedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the invoice calendar date captured for the statutory signature.
        /// </summary>
        public DateOnly InvoiceDate { get; set; }

        /// <summary>
        /// Gets or sets the system-entry timestamp captured for the statutory signature.
        /// </summary>
        public DateTimeOffset SystemEntryDate { get; set; }

        /// <summary>
        /// Gets or sets the persisted fiscal signature hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the persisted fiscal signature control value.
        /// </summary>
        public string HashControl { get; set; }

        /// <summary>
        /// Gets or sets the persisted predecessor hash.
        /// </summary>
        public string PreviousHash { get; set; }

        /// <summary>
        /// Gets or sets the non-secret signing key identifier used by the document.
        /// </summary>
        public string SigningKeyIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the non-secret signing key version used by the document.
        /// </summary>
        public string SigningKeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the signing algorithm used by the document.
        /// </summary>
        public string SigningAlgorithm { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the QR payload generated from the final fiscal snapshot.
        /// </summary>
        public string QrPayload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the PNG generated from the final fiscal QR payload.
        /// </summary>
        public byte[] QrPng { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the software certificate number captured at issuance.
        /// </summary>
        public string SoftwareCertificateNumber { get; set; }

        /// <summary>
        /// Gets or sets the issuer legal name captured at issuance.
        /// </summary>
        public string IssuerLegalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the issuer tax registration number captured at issuance.
        /// </summary>
        public string IssuerTaxRegistrationNumber { get; set; }

        /// <summary>
        /// Gets or sets the issuer legal address captured at issuance.
        /// </summary>
        public string IssuerLegalAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the donor name captured at issuance.
        /// </summary>
        public string DonorName { get; set; }

        /// <summary>
        /// Gets or sets the donor tax registration number captured at issuance.
        /// </summary>
        public string DonorTaxRegistrationNumber { get; set; }

        /// <summary>
        /// Gets or sets the tax rule captured at issuance.
        /// </summary>
        public string TaxRule { get; set; }

        /// <summary>
        /// Gets or sets the net total.
        /// </summary>
        public decimal NetTotal { get; set; }

        /// <summary>
        /// Gets or sets the tax total.
        /// </summary>
        public decimal TaxTotal { get; set; }

        /// <summary>
        /// Gets or sets the gross total.
        /// </summary>
        public decimal GrossTotal { get; set; }

        /// <summary>
        /// Gets or sets the blob name of the immutable fiscal PDF.
        /// </summary>
        public Guid? PdfBlobName { get; set; }

        /// <summary>
        /// Gets or sets the checksum of the immutable fiscal PDF.
        /// </summary>
        public string PdfChecksum { get; set; }

        /// <summary>
        /// Gets or sets the lines captured at issuance.
        /// </summary>
        public ICollection<FiscalDocumentLine> Lines { get; set; }

        /// <summary>
        /// Gets or sets the document lifecycle events.
        /// </summary>
        public ICollection<FiscalDocumentEvent> Events { get; set; }
    }
}
