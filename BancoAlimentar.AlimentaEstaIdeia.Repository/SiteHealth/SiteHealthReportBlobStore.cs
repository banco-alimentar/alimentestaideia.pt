// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportBlobStore.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;

    /// <summary>
    /// Persists site health report snapshots to Azure Blob Storage.
    /// </summary>
    public class SiteHealthReportBlobStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        /// <summary>
        /// Loads the latest report, if present.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Report or null.</returns>
        public async Task<SiteHealthReport> LoadReportAsync(
            string connectionString,
            string containerName,
            CancellationToken cancellationToken = default)
        {
            return await this.LoadJsonAsync<SiteHealthReport>(
                connectionString,
                containerName,
                SiteHealthReportPaths.ReportBlobName,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves the latest report snapshot.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="report">Report to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task SaveReportAsync(
            string connectionString,
            string containerName,
            SiteHealthReport report,
            CancellationToken cancellationToken = default)
        {
            await this.SaveJsonAsync(
                connectionString,
                containerName,
                SiteHealthReportPaths.ReportBlobName,
                report,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads generation status, if present.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Status or null.</returns>
        public async Task<SiteHealthReportGenerationStatus> LoadGenerationStatusAsync(
            string connectionString,
            string containerName,
            CancellationToken cancellationToken = default)
        {
            return await this.LoadJsonAsync<SiteHealthReportGenerationStatus>(
                connectionString,
                containerName,
                SiteHealthReportPaths.GenerationStatusBlobName,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves generation status.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="status">Status to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task SaveGenerationStatusAsync(
            string connectionString,
            string containerName,
            SiteHealthReportGenerationStatus status,
            CancellationToken cancellationToken = default)
        {
            await this.SaveJsonAsync(
                connectionString,
                containerName,
                SiteHealthReportPaths.GenerationStatusBlobName,
                status,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> LoadJsonAsync<T>(
            string connectionString,
            string containerName,
            string blobName,
            CancellationToken cancellationToken)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            BlobContainerClient container = await this.GetContainerAsync(connectionString, containerName, cancellationToken)
                .ConfigureAwait(false);
            BlobClient blob = container.GetBlobClient(blobName);
            if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            BlobDownloadInfo download = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
            using StreamReader reader = new StreamReader(download.Content, Encoding.UTF8);
            string json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        private async Task SaveJsonAsync<T>(
            string connectionString,
            string containerName,
            string blobName,
            T payload,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
            }

            BlobContainerClient container = await this.GetContainerAsync(connectionString, containerName, cancellationToken)
                .ConfigureAwait(false);
            BlobClient blob = container.GetBlobClient(blobName);
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using MemoryStream stream = new MemoryStream(bytes);
            BlobUploadOptions options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/json; charset=utf-8",
                    CacheControl = "no-cache",
                },
            };

            await blob.UploadAsync(stream, options, cancellationToken).ConfigureAwait(false);
        }

        private async Task<BlobContainerClient> GetContainerAsync(
            string connectionString,
            string containerName,
            CancellationToken cancellationToken)
        {
            BlobServiceClient service = new BlobServiceClient(connectionString);
            BlobContainerClient container = service.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return container;
        }
    }
}
