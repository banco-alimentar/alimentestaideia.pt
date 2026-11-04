// -----------------------------------------------------------------------
// <copyright file="TenantStaticLocalCacheService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;

    /// <summary>
    /// Manages the on-disk cache of tenant static files copied from blob storage.
    /// </summary>
    public class TenantStaticLocalCacheService : ITenantStaticLocalCacheService
    {
        private const string VersionManifestName = ".tenant-static-versions.json";
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> cacheLocks = new ConcurrentDictionary<Guid, SemaphoreSlim>();
        private readonly Func<string, string, BlobContainerClient> createContainerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticLocalCacheService"/> class.
        /// </summary>
        public TenantStaticLocalCacheService()
            : this((connectionString, containerName) => new BlobContainerClient(connectionString, containerName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticLocalCacheService"/> class.
        /// </summary>
        /// <param name="createContainerClient">Storage client factory.</param>
        public TenantStaticLocalCacheService(Func<string, string, BlobContainerClient> createContainerClient)
        {
            ArgumentNullException.ThrowIfNull(createContainerClient);
            this.createContainerClient = createContainerClient;
        }

        /// <inheritdoc/>
        public TenantStaticLocalCacheResult Clear(Guid tenantPublicId)
        {
            SemaphoreSlim cacheLock = this.GetCacheLock(tenantPublicId);
            if (!cacheLock.Wait(0))
            {
                throw new InvalidOperationException("Tenant static cache synchronization is in progress. Retry after it completes.");
            }

            try
            {
                string cacheRootPath = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(tenantPublicId);
                int filesRemoved = 0;
                if (Directory.Exists(cacheRootPath))
                {
                    filesRemoved = Directory.EnumerateFiles(cacheRootPath, "*", SearchOption.AllDirectories)
                        .Count(path => Path.GetFileName(path) != VersionManifestName);
                    Directory.Delete(cacheRootPath, recursive: true);
                }

                return new TenantStaticLocalCacheResult { CacheRootPath = cacheRootPath, FilesRemoved = filesRemoved };
            }
            finally
            {
                cacheLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<TenantStaticLocalCacheResult> ResyncFromBlobAsync(
            Guid tenantPublicId,
            string blobContainerName,
            string storageConnectionString,
            bool onlyIfSizeChanged = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storageConnectionString))
            {
                throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
            }

            if (string.IsNullOrWhiteSpace(blobContainerName))
            {
                throw new InvalidOperationException("Tenant blob container name is not configured.");
            }

            SemaphoreSlim cacheLock = this.GetCacheLock(tenantPublicId);
            await cacheLock.WaitAsync(cancellationToken);
            try
            {
                return await this.SynchronizeAsync(tenantPublicId, blobContainerName, storageConnectionString, onlyIfSizeChanged, cancellationToken);
            }
            finally
            {
                cacheLock.Release();
            }
        }

        private static async Task<Dictionary<string, string>> ReadVersionsAsync(string path, CancellationToken cancellationToken)
        {
            if (File.Exists(path))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(path, cancellationToken);
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                catch (JsonException)
                {
                    return new Dictionary<string, string>();
                }
            }

            return new Dictionary<string, string>();
        }

        private static async Task WriteVersionsAsync(string path, Dictionary<string, string> versions, CancellationToken cancellationToken)
        {
            string temporaryFile = string.Concat(path, ".", Guid.NewGuid().ToString("N"), ".download");
            try
            {
                await File.WriteAllTextAsync(temporaryFile, JsonSerializer.Serialize(versions), cancellationToken);
                File.Move(temporaryFile, path, overwrite: true);
            }
            finally
            {
                File.Delete(temporaryFile);
            }
        }

        private async Task<TenantStaticLocalCacheResult> SynchronizeAsync(
            Guid tenantPublicId,
            string blobContainerName,
            string storageConnectionString,
            bool onlyIfSizeChanged,
            CancellationToken cancellationToken)
        {
            string cacheRootPath = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(tenantPublicId);
            BlobContainerClient containerClient = this.createContainerClient(storageConnectionString, blobContainerName);
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                return new TenantStaticLocalCacheResult
                {
                    CacheRootPath = cacheRootPath,
                };
            }

            Directory.CreateDirectory(cacheRootPath);
            string manifestPath = Path.Combine(cacheRootPath, VersionManifestName);
            Dictionary<string, string> previousVersions = await ReadVersionsAsync(manifestPath, cancellationToken);
            var currentVersions = new Dictionary<string, string>(StringComparer.Ordinal);
            var currentFiles = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            int filesDownloaded = 0;
            await foreach (BlobItem blob in containerClient.GetBlobsAsync(
                BlobTraits.Metadata,
                BlobStates.None,
                "wwwroot/",
                cancellationToken))
            {
                BlobBaseClient blobClient = containerClient.GetBlobClient(blob.Name);
                string targetFile = StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(tenantPublicId, blob.Name);
                currentFiles.Add(targetFile);
                string version = blob.Properties.ETag?.ToString() ?? blob.Properties.LastModified?.ToString("O") ?? string.Empty;
                currentVersions[blob.Name] = version;
                if (onlyIfSizeChanged && version.Length > 0 && File.Exists(targetFile) &&
                    new FileInfo(targetFile).Length == blob.Properties.ContentLength &&
                    previousVersions.TryGetValue(blob.Name, out string? previousVersion) && previousVersion == version)
                {
                    continue;
                }

                string? directory = Path.GetDirectoryName(targetFile);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string temporaryFile = string.Concat(targetFile, ".", Guid.NewGuid().ToString("N"), ".download");
                try
                {
                    var downloadOptions = new BlobDownloadToOptions
                    {
                        Conditions = blob.Properties.ETag.HasValue ? new BlobRequestConditions { IfMatch = blob.Properties.ETag.Value } : null,
                    };
                    await blobClient.DownloadToAsync(temporaryFile, downloadOptions, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(temporaryFile, targetFile, overwrite: true);
                }
                finally
                {
                    File.Delete(temporaryFile);
                }

                filesDownloaded++;
            }

            int filesRemoved = 0;
            string webRoot = Path.Combine(cacheRootPath, "wwwroot");
            if (Directory.Exists(webRoot))
            {
                foreach (string file in Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories))
                {
                    if (!currentFiles.Contains(file))
                    {
                        File.Delete(file);
                        filesRemoved++;
                    }
                }
            }

            await WriteVersionsAsync(manifestPath, currentVersions, cancellationToken);
            return new TenantStaticLocalCacheResult
            {
                CacheRootPath = cacheRootPath,
                FilesDownloaded = filesDownloaded,
                FilesRemoved = filesRemoved,
            };
        }

        private SemaphoreSlim GetCacheLock(Guid tenantPublicId)
        {
            return this.cacheLocks.GetOrAdd(tenantPublicId, _ => new SemaphoreSlim(1, 1));
        }
    }
}
