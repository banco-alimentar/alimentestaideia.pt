namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using Moq;
    using Xunit;

    /// <summary>
    /// Tests tenant static cache paths and synchronization.
    /// </summary>
    public class TenantStaticLocalCacheServiceTest : IDisposable
    {
        private readonly Guid tenantId = Guid.NewGuid();

        /// <summary>
        /// Blob names cannot escape the tenant cache root.
        /// </summary>
        /// <param name="path">Unsafe blob path.</param>
        [Theory]
        [InlineData("../outside.txt")]
        [InlineData("wwwroot/../../outside.txt")]
        [InlineData("wwwroot\\..\\..\\outside.txt")]
        [InlineData("/outside.txt")]
        public void CachePathRejectsEscapingNames(string path)
        {
            Assert.Throws<ArgumentException>(() => StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(Guid.NewGuid(), path));
        }

        /// <summary>
        /// Valid relative paths stay under the tenant cache root.
        /// </summary>
        [Fact]
        public void CachePathPreservesRelativeDirectories()
        {
            Guid tenantId = Guid.NewGuid();
            string path = StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(tenantId, "wwwroot/css/site.css");

            Assert.Equal(Path.Combine(StaticFileConfigurationManager.GetTenantLocalCacheRootPath(tenantId), "wwwroot", "css", "site.css"), path);
        }

        /// <summary>
        /// Same-sized changes are downloaded and deleted blobs are removed from the cache.
        /// </summary>
        [Fact]
        public async Task CacheTracksVersionsAndDeletedBlobs()
        {
            var blobs = new List<BlobItem> { CreateBlob("v1", 4) };
            var blobClient = new Mock<BlobClient>();
            string content = "aaaa";
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, content, cancellationToken);
                    return Mock.Of<Response>();
                });
            Mock<BlobContainerClient> container = CreateContainer(blobs, blobClient.Object);
            var service = new TenantStaticLocalCacheService((connection, name) => container.Object);
            string target = StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(this.tenantId, "wwwroot/site.css");

            Assert.Equal(1, (await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true)).FilesDownloaded);
            Assert.Equal(0, (await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true)).FilesDownloaded);
            content = "bbbb";
            blobs[0] = CreateBlob("v2", 4);
            Assert.Equal(1, (await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true)).FilesDownloaded);
            Assert.Equal("bbbb", await File.ReadAllTextAsync(target));

            blobs.Clear();
            Assert.Equal(1, (await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true)).FilesRemoved);
            Assert.False(File.Exists(target));
        }

        /// <summary>
        /// A failed staged download leaves the previous cached content intact.
        /// </summary>
        [Fact]
        public async Task FailedDownloadPreservesCachedFile()
        {
            var blobs = new List<BlobItem> { CreateBlob("v1", 4) };
            var blobClient = new Mock<BlobClient>();
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "aaaa", cancellationToken);
                    return Mock.Of<Response>();
                });
            Mock<BlobContainerClient> container = CreateContainer(blobs, blobClient.Object);
            var service = new TenantStaticLocalCacheService((connection, name) => container.Object);
            await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            TenantStaticSyncStatus successfulStatus = service.GetStatus(this.tenantId);
            blobs[0] = CreateBlob("v2", 4);
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "partial", cancellationToken);
                    throw new IOException("Synthetic interrupted download.");
                });

            await Assert.ThrowsAsync<IOException>(() => service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true));
            string target = StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(this.tenantId, "wwwroot/site.css");
            Assert.Equal("aaaa", await File.ReadAllTextAsync(target));
            Assert.Empty(Directory.EnumerateFiles(StaticFileConfigurationManager.GetTenantLocalCacheRootPath(this.tenantId), "*.download", SearchOption.AllDirectories));
            TenantStaticSyncStatus failedStatus = service.GetStatus(this.tenantId);
            Assert.Equal(successfulStatus.LastSuccessAt, failedStatus.LastSuccessAt);
            Assert.True(failedStatus.LastAttemptAt >= successfulStatus.LastAttemptAt);
            Assert.Equal("LocalIO", failedStatus.LastFailureType);
        }

        /// <summary>
        /// A later failure must not replace earlier known-good assets or their manifest.
        /// </summary>
        /// <param name="failDuringPublication">Whether the failure occurs after downloads complete.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedPassPreservesAllAssetsAndManifest(bool failDuringPublication)
        {
            var blobs = new List<BlobItem> { CreateBlob("v1", 4) };
            var blobClient = new Mock<BlobClient>();
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "aaaa", cancellationToken);
                    return Mock.Of<Response>();
                });
            Mock<BlobContainerClient> container = CreateContainer(blobs, blobClient.Object);
            var service = new TenantStaticLocalCacheService((connection, name) => container.Object);
            await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            string root = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(this.tenantId);
            string manifest = await File.ReadAllTextAsync(Path.Combine(root, ".tenant-static-versions.json"));
            string obsolete = Path.Combine(root, "wwwroot", "obsolete.css");
            await File.WriteAllTextAsync(obsolete, "known-good");
            blobs[0] = CreateBlob("v2", 4);
            blobs.Add(BlobsModelFactory.BlobItem("wwwroot/later.css", properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: false, contentLength: 4, eTag: new ETag("v2"))));
            var laterBlob = new Mock<BlobClient>();
            if (failDuringPublication)
            {
                // A blob that needs later.css to be a directory conflicts with the later.css blob itself.
                blobs.Add(BlobsModelFactory.BlobItem("wwwroot/later.css/nested.css", properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: false, contentLength: 4, eTag: new ETag("v2"))));
                container.Setup(item => item.GetBlobClient("wwwroot/later.css/nested.css")).Returns(laterBlob.Object);
                laterBlob.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                    {
                        await File.WriteAllTextAsync(path, "dddd", cancellationToken);
                        return Mock.Of<Response>();
                    });
            }
            else
            {
                laterBlob.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RequestFailedException(503, "Synthetic private storage detail."));
            }

            container.Setup(item => item.GetBlobClient("wwwroot/later.css")).Returns(laterBlob.Object);
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "bbbb", cancellationToken);
                    return Mock.Of<Response>();
                });

            if (failDuringPublication)
            {
                await Assert.ThrowsAsync<IOException>(() => service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true));
            }
            else
            {
                await Assert.ThrowsAsync<RequestFailedException>(() => service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true));
            }

            Assert.Equal("aaaa", await File.ReadAllTextAsync(Path.Combine(root, "wwwroot", "site.css")));
            Assert.Equal("known-good", await File.ReadAllTextAsync(obsolete));
            Assert.False(File.Exists(Path.Combine(root, "wwwroot", "later.css")));
            Assert.False(Directory.Exists(Path.Combine(root, "wwwroot", "later.css")));
            Assert.Equal(manifest, await File.ReadAllTextAsync(Path.Combine(root, ".tenant-static-versions.json")));
            Assert.Equal(failDuringPublication ? "LocalIO" : "StorageUnavailable", service.GetStatus(this.tenantId).LastFailureType);
            Assert.Empty(Directory.EnumerateFiles(root, "*.download", SearchOption.AllDirectories));
            Assert.Empty(Directory.EnumerateFiles(root, "*.backup", SearchOption.AllDirectories));
        }

        /// <summary>
        /// Missing storage is a failed refresh and cannot replace a previous success.
        /// </summary>
        [Fact]
        public async Task MissingContainerPreservesSuccessfulStatusAndFiles()
        {
            var blobs = new List<BlobItem> { CreateBlob("v1", 4) };
            var blobClient = new Mock<BlobClient>();
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "aaaa", cancellationToken);
                    return Mock.Of<Response>();
                });
            Mock<BlobContainerClient> container = CreateContainer(blobs, blobClient.Object);
            var service = new TenantStaticLocalCacheService((connection, name) => container.Object);
            await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            DateTimeOffset? successAt = service.GetStatus(this.tenantId).LastSuccessAt;
            container.Setup(item => item.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true));

            Assert.Equal(successAt, service.GetStatus(this.tenantId).LastSuccessAt);
            Assert.Equal("ConfigurationUnavailable", service.GetStatus(this.tenantId).LastFailureType);
            Assert.Equal("aaaa", await File.ReadAllTextAsync(StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(this.tenantId, "wwwroot/site.css")));
        }

        /// <summary>
        /// Cancellation cleans staging, retains the successful snapshot and releases the lock.
        /// </summary>
        [Fact]
        public async Task CanceledDownloadRetainsAssetsAndAllowsRetry()
        {
            var blobs = new List<BlobItem> { CreateBlob("v1", 4) };
            var blobClient = new Mock<BlobClient>();
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "aaaa", cancellationToken);
                    return Mock.Of<Response>();
                });
            Mock<BlobContainerClient> container = CreateContainer(blobs, blobClient.Object);
            var service = new TenantStaticLocalCacheService((connection, name) => container.Object);
            await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            DateTimeOffset? successAt = service.GetStatus(this.tenantId).LastSuccessAt;
            using var cancellation = new CancellationTokenSource();
            blobs[0] = CreateBlob("v2", 4);
            blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                {
                    await File.WriteAllTextAsync(path, "partial", cancellationToken);
                    cancellation.Cancel();
                    cancellationToken.ThrowIfCancellationRequested();
                    return Mock.Of<Response>();
                });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true, cancellation.Token));

            Assert.Equal(successAt, service.GetStatus(this.tenantId).LastSuccessAt);
            Assert.Equal("Canceled", service.GetStatus(this.tenantId).LastFailureType);
            Assert.Equal("aaaa", await File.ReadAllTextAsync(StaticFileConfigurationManager.GetTenantLocalTemporalFilePath(this.tenantId, "wwwroot/site.css")));
            Assert.Empty(Directory.EnumerateFiles(StaticFileConfigurationManager.GetTenantLocalCacheRootPath(this.tenantId), "*.download", SearchOption.AllDirectories));
            blobs.Clear();
            await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            Assert.Null(service.GetStatus(this.tenantId).LastFailureType);
        }

        /// <summary>
        /// A cached file can become a directory and back without manual cache clearing.
        /// </summary>
        [Fact]
        public async Task LayoutChangesBetweenFileAndDirectoryArePublished()
        {
            var blobs = new List<BlobItem> { CreateBlob("wwwroot/icons", "v1") };
            var container = new Mock<BlobContainerClient>();
            container.Setup(item => item.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
            container.Setup(item => item.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, "wwwroot/", It.IsAny<CancellationToken>()))
                .Returns(() => AsyncPageable<BlobItem>.FromPages(new[] { Page<BlobItem>.FromValues(blobs.ToArray(), null, Mock.Of<Response>()) }));
            container.Setup(item => item.GetBlobClient(It.IsAny<string>())).Returns<string>(name =>
            {
                var blobClient = new Mock<BlobClient>();
                blobClient.Setup(item => item.DownloadToAsync(It.IsAny<string>(), It.IsAny<BlobDownloadToOptions>(), It.IsAny<CancellationToken>()))
                    .Returns<string, BlobDownloadToOptions, CancellationToken>(async (path, options, cancellationToken) =>
                    {
                        await File.WriteAllTextAsync(path, name, cancellationToken);
                        return Mock.Of<Response>();
                    });
                return blobClient.Object;
            });
            var service = new TenantStaticLocalCacheService((connection, name) => container.Object);
            string root = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(this.tenantId);
            string icons = Path.Combine(root, "wwwroot", "icons");
            string logo = Path.Combine(icons, "nested", "logo.svg");
            await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            Assert.Equal("wwwroot/icons", await File.ReadAllTextAsync(icons));

            blobs[0] = CreateBlob("wwwroot/icons/nested/logo.svg", "v2");
            TenantStaticLocalCacheResult toDirectory = await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            Assert.Equal("wwwroot/icons/nested/logo.svg", await File.ReadAllTextAsync(logo));
            Assert.Equal(1, toDirectory.FilesRemoved);

            blobs[0] = CreateBlob("wwwroot/icons", "v3");
            TenantStaticLocalCacheResult toFile = await service.ResyncFromBlobAsync(this.tenantId, "tenant", "test-only", true);
            Assert.Equal("wwwroot/icons", await File.ReadAllTextAsync(icons));
            Assert.Equal(1, toFile.FilesRemoved);
            Assert.Null(service.GetStatus(this.tenantId).LastFailureType);
            Assert.Empty(Directory.EnumerateFiles(root, "*.download", SearchOption.AllDirectories));
            Assert.Empty(Directory.EnumerateFiles(root, "*.backup", SearchOption.AllDirectories));
        }

        /// <summary>
        /// File metadata and repeated existence checks do not repeat storage requests.
        /// </summary>
        [Fact]
        public void FileInfoReusesBlobMetadata()
        {
            var blob = new Mock<BlobBaseClient>();
            blob.SetupGet(item => item.Name).Returns("wwwroot/site.css");
            BlobProperties properties = BlobsModelFactory.BlobProperties(contentLength: 4, lastModified: DateTimeOffset.UtcNow);
            blob.Setup(item => item.GetProperties(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .Returns(Response.FromValue(properties, Mock.Of<Response>()));

            var file = new TenantStaticFileInfo(blob.Object);
            Assert.True(file.Exists);
            Assert.True(file.Exists);
            Assert.Equal(4, file.Length);
            Assert.Equal("site.css", file.Name);
            blob.Verify(item => item.GetProperties(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
            blob.Verify(item => item.Exists(It.IsAny<CancellationToken>()), Times.Never);

            var fromListing = new TenantStaticFileInfo(blob.Object, properties);
            Assert.True(fromListing.Exists);
            blob.Verify(item => item.GetProperties(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            string root = StaticFileConfigurationManager.GetTenantLocalCacheRootPath(this.tenantId);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static BlobItem CreateBlob(string version, long length)
        {
            return BlobsModelFactory.BlobItem("wwwroot/site.css", properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: false, contentLength: length, eTag: new ETag(version)));
        }

        private static BlobItem CreateBlob(string name, string version)
        {
            return BlobsModelFactory.BlobItem(name, properties: BlobsModelFactory.BlobItemProperties(accessTierInferred: false, contentLength: name.Length, eTag: new ETag(version)));
        }

        private static Mock<BlobContainerClient> CreateContainer(List<BlobItem> blobs, BlobClient blobClient)
        {
            var container = new Mock<BlobContainerClient>();
            container.Setup(item => item.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));
            container.Setup(item => item.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, "wwwroot/", It.IsAny<CancellationToken>()))
                .Returns(() => AsyncPageable<BlobItem>.FromPages(new[] { Page<BlobItem>.FromValues(blobs.ToArray(), null, Mock.Of<Response>()) }));
            container.Setup(item => item.GetBlobClient("wwwroot/site.css")).Returns(blobClient);
            return container;
        }
    }
}