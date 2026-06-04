// -----------------------------------------------------------------------
// <copyright file="TenantStaticLocalCacheService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider
{
    using System;
    using System.IO;
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
        /// <inheritdoc/>
        public TenantStaticLocalCacheResult Clear(Guid tenantPublicId)
        {
            string cacheRootPath = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(tenantPublicId);
            int filesRemoved = 0;
            if (Directory.Exists(cacheRootPath))
            {
                filesRemoved = Directory.GetFiles(cacheRootPath, "*", SearchOption.AllDirectories).Length;
                Directory.Delete(cacheRootPath, recursive: true);
            }

            return new TenantStaticLocalCacheResult
            {
                CacheRootPath = cacheRootPath,
                FilesRemoved = filesRemoved,
            };
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

            string cacheRootPath = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(tenantPublicId);
            BlobContainerClient containerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                return new TenantStaticLocalCacheResult
                {
                    CacheRootPath = cacheRootPath,
                };
            }

            int filesDownloaded = 0;
            await foreach (BlobItem blob in containerClient.GetBlobsAsync(
                BlobTraits.Metadata,
                BlobStates.None,
                "wwwroot/",
                cancellationToken))
            {
                BlobBaseClient blobClient = containerClient.GetBlobBaseClient(blob.Name);
                string targetFile = StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(tenantPublicId, blob.Name);
                bool needUpdate = !onlyIfSizeChanged;
                if (onlyIfSizeChanged)
                {
                    if (!File.Exists(targetFile))
                    {
                        needUpdate = true;
                    }
                    else
                    {
                        FileInfo fileInfo = new FileInfo(targetFile);
                        needUpdate = fileInfo.Length != blob.Properties.ContentLength;
                    }
                }

                if (!needUpdate)
                {
                    continue;
                }

                string? directory = Path.GetDirectoryName(targetFile);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await blobClient.DownloadToAsync(targetFile, cancellationToken);
                filesDownloaded++;
            }

            return new TenantStaticLocalCacheResult
            {
                CacheRootPath = cacheRootPath,
                FilesDownloaded = filesDownloaded,
            };
        }
    }
}
