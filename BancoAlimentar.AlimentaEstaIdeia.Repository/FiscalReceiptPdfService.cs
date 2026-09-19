// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptPdfService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Renders and persists exactly one immutable PDF for an issued fiscal document.
    /// </summary>
    public class FiscalReceiptPdfService
    {
        private readonly IFiscalReceiptPdfBlobStore blobStore;
        private readonly IFiscalReceiptPdfRenderer renderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalReceiptPdfService"/> class.
        /// </summary>
        /// <param name="renderer">The receipt renderer.</param>
        /// <param name="blobStore">The immutable artifact store.</param>
        public FiscalReceiptPdfService(
            IFiscalReceiptPdfRenderer renderer,
            IFiscalReceiptPdfBlobStore blobStore)
        {
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            this.blobStore = blobStore ?? throw new ArgumentNullException(nameof(blobStore));
        }

        /// <summary>
        /// Returns the existing immutable artifact, or renders and persists the initial artifact.
        /// </summary>
        /// <param name="document">The issued fiscal document that records artifact metadata.</param>
        /// <param name="viewModel">The final persisted fiscal receipt model.</param>
        /// <returns>The immutable fiscal PDF artifact.</returns>
        public FiscalReceiptPdfArtifact GetOrCreate(FiscalDocument document, FiscalReceiptViewModel viewModel)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (document.PdfBlobName.HasValue || !string.IsNullOrWhiteSpace(document.PdfChecksum))
            {
                return this.LoadPersistedArtifact(document);
            }

            this.ValidateTemplateVersion(document, viewModel);

            byte[] content = this.renderer.Render(viewModel);
            if (content == null || content.Length == 0)
            {
                throw new InvalidOperationException("Fiscal PDF rendering produced no content.");
            }

            Guid blobName = Guid.NewGuid();
            string checksum = Convert.ToHexString(SHA256.HashData(content));
            try
            {
                this.blobStore.Save(blobName, content);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Fiscal PDF blob persistence failed.", exception);
            }

            document.PdfBlobName = blobName;
            document.PdfChecksum = checksum;
            document.PdfGeneratedAtUtc = DateTimeOffset.UtcNow;

            return this.CreateArtifact(blobName, checksum, content, document.PdfTemplateVersion);
        }

        private FiscalReceiptPdfArtifact LoadPersistedArtifact(FiscalDocument document)
        {
            if (!document.PdfBlobName.HasValue || string.IsNullOrWhiteSpace(document.PdfChecksum))
            {
                throw new InvalidOperationException("Fiscal PDF artifact metadata is incomplete.");
            }

            byte[] content;
            try
            {
                content = this.blobStore.Load(document.PdfBlobName.Value);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Fiscal PDF blob retrieval failed.", exception);
            }

            if (content == null || content.Length == 0 ||
                !string.Equals(
                    document.PdfChecksum,
                    Convert.ToHexString(SHA256.HashData(content)),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fiscal PDF blob content does not match its persisted checksum.");
            }

            return this.CreateArtifact(
                document.PdfBlobName.Value,
                document.PdfChecksum,
                content,
                document.PdfTemplateVersion);
        }

        private void ValidateTemplateVersion(FiscalDocument document, FiscalReceiptViewModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(document.PdfTemplateVersion))
            {
                throw new InvalidOperationException("Fiscal PDF requires a persisted template version.");
            }

            if (!string.Equals(document.PdfTemplateVersion, viewModel.TemplateVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fiscal PDF template version does not match the persisted document.");
            }
        }

        private FiscalReceiptPdfArtifact CreateArtifact(Guid blobName, string checksum, byte[] content, string templateVersion)
        {
            return new FiscalReceiptPdfArtifact
            {
                BlobName = blobName,
                Checksum = checksum,
                Content = content.ToArray(),
                TemplateVersion = templateVersion,
            };
        }
    }
}
