// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptViewModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the complete immutable fiscal snapshot required to render a receipt.
    /// </summary>
    public sealed class FiscalReceiptViewModel
    {
        /// <summary>Gets the persisted issuer legal name.</summary>
        public string IssuerName { get; init; } = string.Empty;

        /// <summary>Gets the persisted issuer tax registration number.</summary>
        public string IssuerTaxRegistrationNumber { get; init; } = string.Empty;

        /// <summary>Gets the persisted issuer legal address.</summary>
        public string IssuerLegalAddress { get; init; } = string.Empty;

        /// <summary>Gets the persisted buyer name.</summary>
        public string BuyerName { get; init; } = string.Empty;

        /// <summary>Gets the persisted buyer tax registration number.</summary>
        public string BuyerTaxRegistrationNumber { get; init; } = string.Empty;

        /// <summary>Gets the persisted document lines.</summary>
        public IReadOnlyList<FiscalReceiptLineViewModel> Lines { get; init; } = Array.Empty<FiscalReceiptLineViewModel>();

        /// <summary>Gets the persisted net total.</summary>
        public decimal NetTotal { get; init; }

        /// <summary>Gets the persisted tax total.</summary>
        public decimal TaxTotal { get; init; }

        /// <summary>Gets the persisted gross total.</summary>
        public decimal GrossTotal { get; init; }

        /// <summary>Gets the persisted document identifier.</summary>
        public string DocumentIdentifier { get; init; } = string.Empty;

        /// <summary>Gets the persisted ATCUD.</summary>
        public string Atcud { get; init; } = string.Empty;

        /// <summary>Gets the persisted signature control.</summary>
        public string HashControl { get; init; } = string.Empty;

        /// <summary>Gets the persisted software certificate number.</summary>
        public string SoftwareCertificateNumber { get; init; } = string.Empty;

        /// <summary>Gets the persisted fiscal QR payload.</summary>
        public string QrPayload { get; init; } = string.Empty;

        /// <summary>Gets the persisted QR PNG bytes.</summary>
        public byte[] QrPng { get; init; } = Array.Empty<byte>();

        /// <summary>Gets a value indicating whether the persisted document is cancelled.</summary>
        public bool IsCancelled { get; init; }

        /// <summary>Gets the persisted cancellation reason.</summary>
        public string CancellationReason { get; init; } = string.Empty;

        /// <summary>Gets a value indicating whether the persisted document is a correction.</summary>
        public bool IsCorrection { get; init; }

        /// <summary>Gets the persisted original-document identifier for a correction.</summary>
        public string OriginalDocumentIdentifier { get; init; } = string.Empty;

        /// <summary>Gets the immutable fiscal receipt template version.</summary>
        public string TemplateVersion { get; init; } = string.Empty;
    }
}
