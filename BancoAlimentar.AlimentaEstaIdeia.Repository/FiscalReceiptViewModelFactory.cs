// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptViewModelFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Projects a loaded fiscal document's persisted snapshots into a receipt render model.
    /// </summary>
    public class FiscalReceiptViewModelFactory
    {
        /// <summary>
        /// Creates an immutable receipt render model from one loaded fiscal document.
        /// </summary>
        /// <param name="document">The fiscal document with its persisted navigations loaded.</param>
        /// <returns>The immutable receipt render model.</returns>
        public FiscalReceiptViewModel Create(FiscalDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            bool isCancelled = document.Status == FiscalDocumentStatus.Cancelled;
            return new FiscalReceiptViewModel
            {
                IssuerName = document.IssuerLegalName ?? string.Empty,
                IssuerTaxRegistrationNumber = document.IssuerTaxRegistrationNumber ?? string.Empty,
                IssuerLegalAddress = document.IssuerLegalAddress ?? string.Empty,
                BuyerName = document.DonorName ?? string.Empty,
                BuyerTaxRegistrationNumber = document.DonorTaxRegistrationNumber ?? string.Empty,
                Lines = (document.Lines ?? Array.Empty<FiscalDocumentLine>())
                    .OrderBy(line => line.LineNumber)
                    .Select(line => new FiscalReceiptLineViewModel
                    {
                        LineNumber = line.LineNumber,
                        ProductCode = line.ProductCode ?? string.Empty,
                        Description = line.Description ?? string.Empty,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        TaxRate = line.TaxRate,
                        TaxExemptionReason = line.TaxExemptionReason ?? string.Empty,
                        NetTotal = line.NetTotal,
                        TaxTotal = line.TaxTotal,
                        GrossTotal = line.GrossTotal,
                    })
                    .ToArray(),
                NetTotal = document.NetTotal,
                TaxTotal = document.TaxTotal,
                GrossTotal = document.GrossTotal,
                DocumentIdentifier = document.DocumentNumber ?? string.Empty,
                Atcud = document.Atcud ?? string.Empty,
                HashControl = document.HashControl ?? string.Empty,
                SoftwareCertificateNumber = document.SoftwareCertificateNumber ?? string.Empty,
                QrPayload = document.QrPayload ?? string.Empty,
                QrPng = document.QrPng?.ToArray() ?? Array.Empty<byte>(),
                IsCancelled = isCancelled,
                CancellationReason = isCancelled
                    ? document.Events?
                        .Where(item => string.Equals(item.EventType, "Cancelled", StringComparison.Ordinal))
                        .OrderByDescending(item => item.OccurredAtUtc)
                        .Select(item => item.Reason)
                        .FirstOrDefault() ?? string.Empty
                    : string.Empty,
                IsCorrection = document.Kind == FiscalDocumentKind.Correction,
                OriginalDocumentIdentifier = document.OriginalFiscalDocument?.DocumentNumber ?? string.Empty,
                TemplateVersion = document.PdfTemplateVersion ?? string.Empty,
            };
        }
    }
}
