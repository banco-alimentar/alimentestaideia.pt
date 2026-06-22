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
        /// Loads the latest report for a deployment slot, if present.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="slotKey">Deployment slot key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Report or null.</returns>
        public Task<SiteHealthReport> LoadReportForSlotAsync(
            string connectionString,
            string containerName,
            string slotKey,
            CancellationToken cancellationToken = default)
        {
            return this.LoadReportAsync(
                connectionString,
                containerName,
                SiteHealthReportPaths.GetReportBlobName(slotKey),
                cancellationToken);
        }

        /// <summary>
        /// Loads a report blob, if present.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="blobName">Blob name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Report or null.</returns>
        public async Task<SiteHealthReport> LoadReportAsync(
            string connectionString,
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            return await this.LoadJsonAsync<SiteHealthReport>(
                connectionString,
                containerName,
                blobName,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves a report snapshot for a deployment slot.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="slotKey">Deployment slot key.</param>
        /// <param name="report">Report to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public Task SaveReportForSlotAsync(
            string connectionString,
            string containerName,
            string slotKey,
            SiteHealthReport report,
            CancellationToken cancellationToken = default)
        {
            return this.SaveReportAsync(
                connectionString,
                containerName,
                SiteHealthReportPaths.GetReportBlobName(slotKey),
                report,
                cancellationToken);
        }

        /// <summary>
        /// Saves a report snapshot.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="blobName">Blob name.</param>
        /// <param name="report">Report to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task SaveReportAsync(
            string connectionString,
            string containerName,
            string blobName,
            SiteHealthReport report,
            CancellationToken cancellationToken = default)
        {
            await this.SaveJsonAsync(
                connectionString,
                containerName,
                blobName,
                report,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads generation status for a deployment slot, if present.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="slotKey">Deployment slot key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Status or null.</returns>
        public Task<SiteHealthReportGenerationStatus> LoadGenerationStatusForSlotAsync(
            string connectionString,
            string containerName,
            string slotKey,
            CancellationToken cancellationToken = default)
        {
            return this.LoadGenerationStatusAsync(
                connectionString,
                containerName,
                SiteHealthReportPaths.GetGenerationStatusBlobName(slotKey),
                cancellationToken);
        }

        /// <summary>
        /// Loads generation status, if present.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="blobName">Blob name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Status or null.</returns>
        public async Task<SiteHealthReportGenerationStatus> LoadGenerationStatusAsync(
            string connectionString,
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            return await this.LoadJsonAsync<SiteHealthReportGenerationStatus>(
                connectionString,
                containerName,
                blobName,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves generation status for a deployment slot.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="slotKey">Deployment slot key.</param>
        /// <param name="status">Status to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public Task SaveGenerationStatusForSlotAsync(
            string connectionString,
            string containerName,
            string slotKey,
            SiteHealthReportGenerationStatus status,
            CancellationToken cancellationToken = default)
        {
            return this.SaveGenerationStatusAsync(
                connectionString,
                containerName,
                SiteHealthReportPaths.GetGenerationStatusBlobName(slotKey),
                status,
                cancellationToken);
        }

        /// <summary>
        /// Saves generation status.
        /// </summary>
        /// <param name="connectionString">Storage connection string.</param>
        /// <param name="containerName">Blob container.</param>
        /// <param name="blobName">Blob name.</param>
        /// <param name="status">Status to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task SaveGenerationStatusAsync(
            string connectionString,
            string containerName,
            string blobName,
            SiteHealthReportGenerationStatus status,
            CancellationToken cancellationToken = default)
        {
            await this.SaveJsonAsync(
                connectionString,
                containerName,
                blobName,
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
