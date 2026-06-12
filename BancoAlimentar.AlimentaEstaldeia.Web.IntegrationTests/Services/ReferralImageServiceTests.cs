// -----------------------------------------------------------------------
// <copyright file="ReferralImageServiceTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.FileProviders;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ReferralImageService"/>.
    /// </summary>
    public sealed class ReferralImageServiceTests : IDisposable
    {
        private static readonly byte[] MinimalPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        private readonly string webRootPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralImageServiceTests"/> class.
        /// </summary>
        public ReferralImageServiceTests()
        {
            this.webRootPath = Path.Combine(Path.GetTempPath(), "referral-image-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.webRootPath);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(this.webRootPath))
            {
                Directory.Delete(this.webRootPath, recursive: true);
            }
        }

        /// <summary>
        /// ResolveUrl returns null for empty values.
        /// </summary>
        [Fact]
        public void ResolveUrl_ReturnsNull_WhenImageUrlIsEmpty()
        {
            var service = this.CreateService();

            Assert.Null(service.ResolveUrl(null));
            Assert.Null(service.ResolveUrl(string.Empty));
            Assert.Null(service.ResolveUrl("   "));
        }

        /// <summary>
        /// ResolveUrl preserves absolute HTTP URLs.
        /// </summary>
        [Fact]
        public void ResolveUrl_ReturnsAbsoluteUrl_WhenAlreadyAbsolute()
        {
            var service = this.CreateService();
            const string absoluteUrl = "https://cdn.example.test/referrals/campaign.png";

            Assert.Equal(absoluteUrl, service.ResolveUrl(absoluteUrl));
        }

        /// <summary>
        /// ResolveUrl maps local storage keys to wwwroot-relative URLs.
        /// </summary>
        [Fact]
        public void ResolveUrl_ReturnsLocalPath_WhenStoredLocally()
        {
            var service = this.CreateService();

            Assert.Equal(
                "/uploads/referrals/42/sample.png",
                service.ResolveUrl("uploads/referrals/42/sample.png"));
        }

        /// <summary>
        /// UploadAsync rejects unsupported content types.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UploadAsync_Throws_WhenContentTypeIsInvalid()
        {
            var service = this.CreateService();
            var file = this.CreateFormFile(MinimalPng, "image/bmp", "campaign.bmp");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.UploadAsync(7, file));

            Assert.Contains("JPG, PNG, GIF, and WebP", exception.Message);
        }

        /// <summary>
        /// UploadAsync rejects files larger than 2 MB.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UploadAsync_Throws_WhenFileIsTooLarge()
        {
            var service = this.CreateService();
            var oversized = new byte[(2 * 1024 * 1024) + 1];
            var file = this.CreateFormFile(oversized, "image/png", "large.png");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.UploadAsync(7, file));

            Assert.Contains("2 MB", exception.Message);
        }

        /// <summary>
        /// UploadAsync stores files under wwwroot when blob storage is not configured.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task UploadAsync_StoresFileLocally_WhenBlobStorageIsDisabled()
        {
            var service = this.CreateService(azureConnectionString: string.Empty);
            var file = this.CreateFormFile(MinimalPng, "image/png", "campaign.png");

            string storageKey = await service.UploadAsync(15, file);

            Assert.StartsWith("uploads/referrals/15/", storageKey, StringComparison.Ordinal);
            Assert.EndsWith(".png", storageKey, StringComparison.Ordinal);
            string physicalPath = Path.Combine(this.webRootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physicalPath));
            Assert.Equal(MinimalPng.Length, new FileInfo(physicalPath).Length);
        }

        /// <summary>
        /// DeleteAsync removes a locally stored referral image.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task DeleteAsync_RemovesLocalFile()
        {
            var service = this.CreateService(azureConnectionString: string.Empty);
            var file = this.CreateFormFile(MinimalPng, "image/png", "campaign.png");
            string storageKey = await service.UploadAsync(21, file);
            string physicalPath = Path.Combine(this.webRootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physicalPath));

            await service.DeleteAsync(storageKey);

            Assert.False(File.Exists(physicalPath));
        }

        private ReferralImageService CreateService(string azureConnectionString = null)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AzureStorage:ConnectionString"] = azureConnectionString ?? "#{AzureStorage--ConnectionString}#",
                })
                .Build();

            return new ReferralImageService(configuration, new TestWebHostEnvironment { WebRootPath = this.webRootPath });
        }

        private FormFile CreateFormFile(byte[] content, string contentType, string fileName)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "ImageUpload", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType,
            };
        }

        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "ReferralImageServiceTests";

            public IFileProvider ContentRootFileProvider { get; set; }

            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

            public string EnvironmentName { get; set; } = "Development";

            public IFileProvider WebRootFileProvider { get; set; }

            public string WebRootPath { get; set; }
        }
    }
}
