// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalReceiptPdfBlobStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Persists immutable simulation PDFs below a prefix that cannot overlap fiscal receipt artifacts.
    /// </summary>
    public class SimulatedFiscalReceiptPdfBlobStore : ISimulatedFiscalReceiptPdfBlobStore
    {
        private const string DefaultContainerName = "pdf";
        private const string SimulationPrefix = "simulation/development/";
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedFiscalReceiptPdfBlobStore"/> class.
        /// </summary>
        /// <param name="configuration">The current tenant Azure Storage configuration.</param>
        public SimulatedFiscalReceiptPdfBlobStore(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public bool Exists(string artifactReference)
        {
            return this.GetBlobClient(artifactReference).Exists();
        }

        /// <inheritdoc />
        public byte[] Load(string artifactReference)
        {
            return this.GetBlobClient(artifactReference).DownloadContent().Value.Content.ToArray();
        }

        /// <inheritdoc />
        public void Save(string artifactReference, byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                throw new ArgumentException("Simulated receipt PDF content is required.", nameof(content));
            }

            try
            {
                using var stream = new MemoryStream(content, writable: false);
                this.GetBlobClient(artifactReference).Upload(
                    stream,
                    new BlobUploadOptions
                    {
                        Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["issuance-mode"] = "simulation",
                            ["origin-environment"] = "Development",
                        },
                    });
            }
            catch (RequestFailedException exception) when (exception.Status == 409 || exception.Status == 412)
            {
                throw new InvalidOperationException("Simulated receipt artifacts are immutable and cannot be overwritten.", exception);
            }
        }

        private BlobClient GetBlobClient(string artifactReference)
        {
            if (string.IsNullOrWhiteSpace(artifactReference) ||
                !artifactReference.StartsWith(SimulationPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException("A simulation/development artifact reference is required.", nameof(artifactReference));
            }

            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("#{", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Simulated receipt storage requires Azure Storage configuration.");
            }

            string containerName = this.configuration["AzureStorage:FiscalPdfContainerName"] ??
                this.configuration["AzureStorage:PdfContainerName"] ?? DefaultContainerName;
            return new BlobContainerClient(connectionString, containerName).GetBlobClient(artifactReference);
        }
    }
}
