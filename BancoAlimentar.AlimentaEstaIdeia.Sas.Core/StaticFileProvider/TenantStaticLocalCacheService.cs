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
    using Azure;
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
        private readonly TimeProvider timeProvider;
        private readonly ConcurrentDictionary<Guid, TenantStaticSyncStatus> statuses = new ConcurrentDictionary<Guid, TenantStaticSyncStatus>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticLocalCacheService"/> class.
        /// </summary>
        public TenantStaticLocalCacheService()
            : this(TimeProvider.System)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticLocalCacheService"/> class.
        /// </summary>
        /// <param name="timeProvider">Runtime clock.</param>
        public TenantStaticLocalCacheService(TimeProvider timeProvider)
            : this((connectionString, containerName) => new BlobContainerClient(connectionString, containerName), timeProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticLocalCacheService"/> class.
        /// </summary>
        /// <param name="createContainerClient">Storage client factory.</param>
        public TenantStaticLocalCacheService(Func<string, string, BlobContainerClient> createContainerClient)
            : this(createContainerClient, TimeProvider.System)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticLocalCacheService"/> class.
        /// </summary>
        /// <param name="createContainerClient">Storage client factory.</param>
        /// <param name="timeProvider">Runtime clock.</param>
        public TenantStaticLocalCacheService(Func<string, string, BlobContainerClient> createContainerClient, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(createContainerClient);
            ArgumentNullException.ThrowIfNull(timeProvider);
            this.createContainerClient = createContainerClient;
            this.timeProvider = timeProvider;
        }

        /// <inheritdoc/>
        public TenantStaticSyncStatus GetStatus(Guid tenantPublicId)
        {
            return this.statuses.GetOrAdd(tenantPublicId, _ => new TenantStaticSyncStatus());
        }

        /// <inheritdoc/>
        public void RecordFailure(Guid tenantPublicId, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            string failureType = exception switch
            {
                OperationCanceledException => "Canceled",
                RequestFailedException => "StorageUnavailable",
                IOException => "LocalIO",
                UnauthorizedAccessException => "LocalAccessDenied",
                ArgumentException => "InvalidConfiguration",
                InvalidOperationException => "ConfigurationUnavailable",
                _ => "RefreshFailed",
            };
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            this.statuses.AddOrUpdate(
                tenantPublicId,
                _ => new TenantStaticSyncStatus { LastAttemptAt = now, LastFailureType = failureType },
                (_, status) => status with { LastAttemptAt = now, LastFailureType = failureType });
        }

        /// <inheritdoc/>
        public void SetNextAttempt(Guid tenantPublicId, DateTimeOffset? nextAttemptAt)
        {
            this.statuses.AddOrUpdate(
                tenantPublicId,
                _ => new TenantStaticSyncStatus { NextAttemptAt = nextAttemptAt },
                (_, status) => status with { NextAttemptAt = nextAttemptAt });
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
            SemaphoreSlim cacheLock = this.GetCacheLock(tenantPublicId);
            await cacheLock.WaitAsync(cancellationToken);
            try
            {
                DateTimeOffset attemptAt = this.timeProvider.GetUtcNow();
                this.statuses.AddOrUpdate(
                    tenantPublicId,
                    _ => new TenantStaticSyncStatus { LastAttemptAt = attemptAt },
                    (_, status) => status with { LastAttemptAt = attemptAt, NextAttemptAt = null });
                if (string.IsNullOrWhiteSpace(storageConnectionString))
                {
                    throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
                }

                if (string.IsNullOrWhiteSpace(blobContainerName))
                {
                    throw new InvalidOperationException("Tenant blob container name is not configured.");
                }

                TenantStaticLocalCacheResult result = await this.SynchronizeAsync(tenantPublicId, blobContainerName, storageConnectionString, onlyIfSizeChanged, cancellationToken);
                DateTimeOffset successAt = this.timeProvider.GetUtcNow();
                this.statuses.AddOrUpdate(
                    tenantPublicId,
                    _ => new TenantStaticSyncStatus { LastAttemptAt = attemptAt, LastSuccessAt = successAt },
                    (_, status) => status with { LastSuccessAt = successAt, LastFailureType = null });
                return result;
            }
            catch (Exception exception)
            {
                this.RecordFailure(tenantPublicId, exception);
                throw;
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

        private static async Task<string> StageVersionsAsync(string path, Dictionary<string, string> versions, CancellationToken cancellationToken)
        {
            string temporaryFile = string.Concat(path, ".", Guid.NewGuid().ToString("N"), ".download");
            try
            {
                await File.WriteAllTextAsync(temporaryFile, JsonSerializer.Serialize(versions), cancellationToken);
                return temporaryFile;
            }
            catch
            {
                File.Delete(temporaryFile);
                throw;
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
                throw new InvalidOperationException("Tenant blob container does not exist.");
            }

            Directory.CreateDirectory(cacheRootPath);
            string manifestPath = Path.Combine(cacheRootPath, VersionManifestName);
            string webRoot = Path.Combine(cacheRootPath, "wwwroot");
            Dictionary<string, string> previousVersions = await ReadVersionsAsync(manifestPath, cancellationToken);
            var currentVersions = new Dictionary<string, string>(StringComparer.Ordinal);
            StringComparer pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var currentFiles = new HashSet<string>(pathComparer);
            var downloads = new Dictionary<string, string>(pathComparer);
            var backups = new Dictionary<string, string>(pathComparer);
            var addedFiles = new List<string>();
            var createdDirectories = new List<string>();
            var removedDirectories = new List<string>();
            string? stagedManifest = null;
            bool published = false;
            int filesDownloaded = 0;
            try
            {
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

                    // Stage outside the served tree so the live layout is untouched until publication.
                    string temporaryFile = Path.Combine(cacheRootPath, string.Concat(".", Guid.NewGuid().ToString("N"), ".download"));
                    downloads.Add(targetFile, temporaryFile);
                    var downloadOptions = new BlobDownloadToOptions
                    {
                        Conditions = blob.Properties.ETag.HasValue ? new BlobRequestConditions { IfMatch = blob.Properties.ETag.Value } : null,
                    };
                    await blobClient.DownloadToAsync(temporaryFile, downloadOptions, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    filesDownloaded++;
                }

                stagedManifest = await StageVersionsAsync(manifestPath, currentVersions, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                int filesRemoved = 0;
                try
                {
                    // Remove obsolete files first so their paths can become directories (and vice versa).
                    if (Directory.Exists(webRoot))
                    {
                        foreach (string file in Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories).ToList())
                        {
                            if (!currentFiles.Contains(file))
                            {
                                MoveToBackup(file);
                                filesRemoved++;
                            }
                        }
                    }

                    foreach (var download in downloads)
                    {
                        EnsureParentDirectory(download.Key);
                        RemoveEmptyDirectory(download.Key);
                        CopyToBackup(download.Key);
                        File.Move(download.Value, download.Key, overwrite: true);
                    }

                    CopyToBackup(manifestPath);
                    File.Move(stagedManifest, manifestPath, overwrite: true);
                    published = true;
                }
                catch
                {
                    Rollback();
                    throw;
                }

                return new TenantStaticLocalCacheResult
                {
                    CacheRootPath = cacheRootPath,
                    FilesDownloaded = filesDownloaded,
                    FilesRemoved = filesRemoved,
                };
            }
            finally
            {
                foreach (string temporaryFile in downloads.Values)
                {
                    File.Delete(temporaryFile);
                }

                if (stagedManifest != null)
                {
                    File.Delete(stagedManifest);
                }

                if (published)
                {
                    foreach (string backup in backups.Values)
                    {
                        File.Delete(backup);
                    }
                }
            }

            string NewBackupPath() => Path.Combine(cacheRootPath, string.Concat(".", Guid.NewGuid().ToString("N"), ".backup"));

            // Keeps the published file in place while retaining a rollback copy.
            void CopyToBackup(string path)
            {
                if (!File.Exists(path))
                {
                    addedFiles.Add(path);
                    return;
                }

                string backup = NewBackupPath();
                try
                {
                    File.Copy(path, backup);
                }
                catch
                {
                    File.Delete(backup);
                    throw;
                }

                backups.Add(path, backup);
            }

            void MoveToBackup(string path)
            {
                string backup = NewBackupPath();
                File.Move(path, backup);
                backups.Add(path, backup);
            }

            void EnsureParentDirectory(string path)
            {
                var missing = new Stack<string>();
                for (string? directory = Path.GetDirectoryName(path); directory != null && !Directory.Exists(directory); directory = Path.GetDirectoryName(directory))
                {
                    if (File.Exists(directory))
                    {
                        // Obsolete files were already removed, so this file is still published at this path.
                        throw new IOException("Tenant static blobs use the same path for a file and a directory.");
                    }

                    missing.Push(directory);
                }

                foreach (string directory in missing)
                {
                    Directory.CreateDirectory(directory);
                    createdDirectories.Add(directory);
                }
            }

            void RemoveEmptyDirectory(string path)
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                if (Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any())
                {
                    throw new IOException("Tenant static blobs use the same path for a file and a directory.");
                }

                removedDirectories.Add(path);
                removedDirectories.AddRange(Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories));
                Directory.Delete(path, recursive: true);
            }

            void Rollback()
            {
                foreach (string file in addedFiles)
                {
                    File.Delete(file);
                }

                for (int i = createdDirectories.Count - 1; i >= 0; i--)
                {
                    string directory = createdDirectories[i];
                    if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory);
                    }
                }

                foreach (string directory in removedDirectories)
                {
                    Directory.CreateDirectory(directory);
                }

                foreach (var backup in backups)
                {
                    string? directory = Path.GetDirectoryName(backup.Key);
                    if (directory != null)
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Move(backup.Value, backup.Key, overwrite: true);
                }
            }
        }

        private SemaphoreSlim GetCacheLock(Guid tenantPublicId)
        {
            return this.cacheLocks.GetOrAdd(tenantPublicId, _ => new SemaphoreSlim(1, 1));
        }
    }
}
