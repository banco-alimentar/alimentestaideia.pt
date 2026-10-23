// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptPdfBlobStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using System.IO;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Persists immutable fiscal receipt PDFs in the configured Azure Blob Storage container.
    /// </summary>
    public class FiscalReceiptPdfBlobStore : IFiscalReceiptPdfBlobStore
    {
        private const string DefaultContainerName = "pdf";
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalReceiptPdfBlobStore"/> class.
        /// </summary>
        /// <param name="configuration">The current tenant Azure Storage configuration.</param>
        public FiscalReceiptPdfBlobStore(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public byte[] Load(Guid blobName)
        {
            if (blobName == Guid.Empty)
            {
                throw new ArgumentException("Fiscal PDF blob name is required.", nameof(blobName));
            }

            return this.GetBlobClient(blobName).DownloadContent().Value.Content.ToArray();
        }

        /// <inheritdoc />
        public void Save(Guid blobName, byte[] content)
        {
            if (blobName == Guid.Empty)
            {
                throw new ArgumentException("Fiscal PDF blob name is required.", nameof(blobName));
            }

            if (content == null || content.Length == 0)
            {
                throw new ArgumentException("Fiscal PDF content is required.", nameof(content));
            }

            try
            {
                using var stream = new MemoryStream(content, writable: false);
                this.GetBlobClient(blobName).Upload(
                    stream,
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                    });
            }
            catch (RequestFailedException exception) when (exception.Status == 409 || exception.Status == 412)
            {
                throw new InvalidOperationException("Fiscal PDF artifacts are immutable and cannot be overwritten.", exception);
            }
        }

        private BlobClient GetBlobClient(Guid blobName)
        {
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("#{", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fiscal PDF storage requires Azure Storage configuration.");
            }

            string containerName = this.configuration["AzureStorage:FiscalPdfContainerName"] ??
                this.configuration["AzureStorage:PdfContainerName"] ?? DefaultContainerName;
            return new BlobContainerClient(connectionString, containerName)
                .GetBlobClient($"fiscal-receipts/{blobName:N}.pdf");
        }
    }
}
