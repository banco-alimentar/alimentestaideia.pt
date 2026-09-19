// -----------------------------------------------------------------------
// <copyright file="FiscalSignatureService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Produces a fiscal signature over a canonical immutable document snapshot.
    /// </summary>
    public class FiscalSignatureService : IFiscalSignatureService
    {
        private readonly IFiscalSigningKeyProvider fiscalSigningKeyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalSignatureService"/> class.
        /// </summary>
        /// <param name="fiscalSigningKeyProvider">The provider that uses a protected signing key.</param>
        public FiscalSignatureService(IFiscalSigningKeyProvider fiscalSigningKeyProvider)
        {
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

            string canonicalPayload = this.BuildCanonicalPayload(request.FiscalDocument, request.PreviousHash);
            FiscalSigningKeyResult keyResult = this.fiscalSigningKeyProvider.Sign(new FiscalSigningKeyRequest
            {
                CanonicalPayload = canonicalPayload,
                SigningKeyIdentifier = request.SigningKeyIdentifier,
                SigningKeyVersion = request.SigningKeyVersion,
            });

            if (keyResult == null || string.IsNullOrWhiteSpace(keyResult.Hash) ||
                string.IsNullOrWhiteSpace(keyResult.HashControl))
            {
                throw new InvalidOperationException("Fiscal signing provider returned incomplete signature evidence.");
            }

            return new FiscalSignatureResult
            {
                CanonicalPayload = canonicalPayload,
                PreviousHash = request.PreviousHash,
                Hash = keyResult.Hash,
                HashControl = keyResult.HashControl,
                SigningKeyVersion = request.SigningKeyVersion,
            };
        }

        private string BuildCanonicalPayload(FiscalDocument fiscalDocument, string previousHash)
        {
            if (string.IsNullOrWhiteSpace(fiscalDocument.DocumentNumber) || string.IsNullOrWhiteSpace(fiscalDocument.Atcud))
            {
                throw new InvalidOperationException("Fiscal signing requires a document number and ATCUD.");
            }

            var payload = new StringBuilder();
            this.AppendField(payload, "v", "1");
            this.AppendField(payload, "series", fiscalDocument.SeriesId.ToString(CultureInfo.InvariantCulture));
            this.AppendField(payload, "sequence", fiscalDocument.Sequence.ToString(CultureInfo.InvariantCulture));
            this.AppendField(payload, "document", fiscalDocument.DocumentNumber);
            this.AppendField(payload, "atcud", fiscalDocument.Atcud);
            this.AppendField(payload, "issued", fiscalDocument.IssuedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            this.AppendField(payload, "gross", fiscalDocument.GrossTotal.ToString("0.00", CultureInfo.InvariantCulture));
            this.AppendField(payload, "previous", previousHash ?? string.Empty);
            return payload.ToString();
        }

        private void AppendField(StringBuilder payload, string name, string value)
        {
            string canonicalValue = value ?? string.Empty;
            payload.Append(name)
                .Append(':')
                .Append(canonicalValue.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(canonicalValue)
                .Append('\n');
        }
    }
}
