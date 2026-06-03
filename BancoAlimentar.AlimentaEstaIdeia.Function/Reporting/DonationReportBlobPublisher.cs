// -----------------------------------------------------------------------
// <copyright file="DonationReportBlobPublisher.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;

    /// <summary>
    /// Uploads generated static HTML report files to Azure Blob Storage.
    /// </summary>
    public class DonationReportBlobPublisher
    {
        /// <summary>
        /// Uploads report files to blob storage.
        /// </summary>
        /// <param name="connectionString">Azure Storage connection string.</param>
        /// <param name="containerName">Target container.</param>
        /// <param name="blobPrefix">Optional path prefix.</param>
        /// <param name="files">Map of relative blob path to HTML content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of blobs uploaded.</returns>
        public async Task<int> PublishAsync(
            string connectionString,
            string containerName,
            string blobPrefix,
            IReadOnlyDictionary<string, string> files,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
            }

            BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

            string normalizedPrefix = NormalizePrefix(blobPrefix);
            int uploaded = 0;

            foreach (KeyValuePair<string, string> file in files)
            {
                string blobName = normalizedPrefix + file.Key.TrimStart('/');
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                byte[] contentBytes = Encoding.UTF8.GetBytes(file.Value);
                using MemoryStream stream = new MemoryStream(contentBytes);
                BlobUploadOptions options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = GetContentType(file.Key),
                        CacheControl = "public, max-age=300",
                    },
                };

                await blobClient.UploadAsync(stream, options, cancellationToken);
                uploaded++;
            }

            return uploaded;
        }

        private static string NormalizePrefix(string blobPrefix)
        {
            if (string.IsNullOrWhiteSpace(blobPrefix))
            {
                return string.Empty;
            }

            string trimmed = blobPrefix.Trim('/');
            return trimmed.Length == 0 ? string.Empty : trimmed + "/";
        }

        private static string GetContentType(string fileName)
        {
            if (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                return "text/css; charset=utf-8";
            }

            if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return "application/json; charset=utf-8";
            }

            return "text/html; charset=utf-8";
        }
    }
}
