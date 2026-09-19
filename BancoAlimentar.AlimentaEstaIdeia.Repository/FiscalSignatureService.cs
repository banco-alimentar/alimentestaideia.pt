// -----------------------------------------------------------------------
// <copyright file="FiscalSignatureService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Produces a fiscal signature over a canonical immutable document snapshot.
    /// </summary>
    public class FiscalSignatureService : IFiscalSignatureService
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IFiscalSigningKeyProvider fiscalSigningKeyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalSignatureService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant fiscal database.</param>
        /// <param name="fiscalSigningKeyProvider">The provider that uses a protected signing key.</param>
        public FiscalSignatureService(
            ApplicationDbContext applicationDbContext,
            IFiscalSigningKeyProvider fiscalSigningKeyProvider)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.fiscalSigningKeyProvider = fiscalSigningKeyProvider ?? throw new ArgumentNullException(nameof(fiscalSigningKeyProvider));
        }

        /// <inheritdoc />
        public FiscalSignatureResult Sign(FiscalSignatureRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.FiscalDocument == null)
            {
                throw new InvalidOperationException("Fiscal signing requires an immutable fiscal document.");
            }

            if (string.IsNullOrWhiteSpace(request.SigningKeyIdentifier))
            {
                throw new InvalidOperationException("Fiscal signing requires a signing key identifier.");
            }

            if (string.IsNullOrWhiteSpace(request.SigningKeyVersion))
            {
                throw new InvalidOperationException("Fiscal signing requires a signing key version.");
            }

            if (!string.Equals(request.SigningAlgorithm, "RSA", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fiscal signing requires the RSA signing algorithm.");
            }

            this.ValidateFiscalDocument(request.FiscalDocument);
            string previousHash = this.ResolvePreviousHash(request.FiscalDocument);
            string canonicalPayload = this.BuildCanonicalPayload(request.FiscalDocument, previousHash);
            FiscalSigningKeyResult keyResult = this.fiscalSigningKeyProvider.Sign(new FiscalSigningKeyRequest
            {
                CanonicalPayload = canonicalPayload,
                SigningKeyIdentifier = request.SigningKeyIdentifier,
                SigningKeyVersion = request.SigningKeyVersion,
                SigningAlgorithm = request.SigningAlgorithm,
            });

            if (keyResult == null || string.IsNullOrWhiteSpace(keyResult.Hash))
            {
                throw new InvalidOperationException("Fiscal signing provider returned incomplete signature evidence.");
            }

            this.ValidateHashControl(keyResult.HashControl);

            return new FiscalSignatureResult
            {
                CanonicalPayload = canonicalPayload,
                PreviousHash = previousHash,
                Hash = keyResult.Hash,
                HashControl = keyResult.HashControl,
                SigningKeyIdentifier = request.SigningKeyIdentifier,
                SigningKeyVersion = request.SigningKeyVersion,
                SigningAlgorithm = request.SigningAlgorithm,
            };
        }

        private string BuildCanonicalPayload(FiscalDocument fiscalDocument, string previousHash)
        {
            this.GetSignatureDates(fiscalDocument, out DateOnly invoiceDate, out DateTimeOffset systemEntryDate);
            return string.Join(
                ";",
                invoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                systemEntryDate.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                fiscalDocument.DocumentNumber,
                fiscalDocument.GrossTotal.ToString("0.00", CultureInfo.InvariantCulture),
                previousHash ?? string.Empty);
        }

        private string ResolvePreviousHash(FiscalDocument fiscalDocument)
        {
            FiscalDocument predecessor = this.applicationDbContext.FiscalDocuments
                .AsNoTracking()
                .Where(item => item.Id != fiscalDocument.Id &&
                    item.SeriesId == fiscalDocument.SeriesId &&
                    !string.IsNullOrEmpty(item.Hash))
                .OrderByDescending(item => item.IssuedAtUtc)
                .ThenByDescending(item => item.Sequence)
                .FirstOrDefault();

            return predecessor?.Hash ?? string.Empty;
        }

        private void ValidateFiscalDocument(FiscalDocument fiscalDocument)
        {
            if (fiscalDocument.SeriesId <= 0 || fiscalDocument.Sequence <= 0 ||
                string.IsNullOrWhiteSpace(fiscalDocument.DocumentNumber))
            {
                throw new InvalidOperationException("Fiscal signing requires a valid sales invoice document.");
            }

            if (fiscalDocument.IssuedAtUtc == default)
            {
                throw new InvalidOperationException("Fiscal signing requires a valid invoice date.");
            }

            bool hasInvoiceDate = fiscalDocument.InvoiceDate != default;
            bool hasSystemEntryDate = fiscalDocument.SystemEntryDate != default;
            if (hasInvoiceDate != hasSystemEntryDate)
            {
                throw new InvalidOperationException("Fiscal signing requires both invoice and system-entry dates.");
            }

            if (fiscalDocument.GrossTotal < 0)
            {
                throw new InvalidOperationException("Fiscal signing requires a non-negative gross amount.");
            }
        }

        private void GetSignatureDates(
            FiscalDocument fiscalDocument,
            out DateOnly invoiceDate,
            out DateTimeOffset systemEntryDate)
        {
            if (fiscalDocument.InvoiceDate == default && fiscalDocument.SystemEntryDate == default)
            {
                DateTimeOffset issuedAtUtc = fiscalDocument.IssuedAtUtc.ToUniversalTime();
                invoiceDate = DateOnly.FromDateTime(issuedAtUtc.UtcDateTime);
                systemEntryDate = issuedAtUtc;
                return;
            }

            invoiceDate = fiscalDocument.InvoiceDate;
            systemEntryDate = fiscalDocument.SystemEntryDate;
        }

        private void ValidateHashControl(string hashControl)
        {
            if (!long.TryParse(
                    hashControl,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long parsedHashControl) ||
                parsedHashControl <= 0)
            {
                throw new InvalidOperationException("Fiscal signing provider returned an invalid signature control value.");
            }
        }
    }
}
