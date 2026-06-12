// -----------------------------------------------------------------------
// <copyright file="ReferralImageService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Stores and resolves referral campaign images in Azure Blob Storage or local wwwroot.
    /// </summary>
    public class ReferralImageService
    {
        private const int MaxFileSizeBytes = 2 * 1024 * 1024;
        private const string DefaultContainerName = "referral-images";
        private const string LocalUploadFolder = "uploads/referrals";

        private static readonly Dictionary<string, string> AllowedContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "image/jpeg", ".jpg" },
            { "image/png", ".png" },
            { "image/gif", ".gif" },
            { "image/webp", ".webp" },
        };

        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralImageService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="webHostEnvironment">Web host environment.</param>
        public ReferralImageService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Uploads a referral image and returns the stored path key.
        /// </summary>
        /// <param name="referralId">Referral identifier.</param>
        /// <param name="file">Uploaded image file.</param>
        /// <returns>Stored image path key.</returns>
        public async Task<string> UploadAsync(int referralId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException("No image file was provided.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException("Referral images must be 2 MB or smaller.");
            }

            if (!AllowedContentTypes.TryGetValue(file.ContentType, out string extension))
            {
                throw new InvalidOperationException("Only JPG, PNG, GIF, and WebP images are allowed.");
            }

            string fileName = $"{Guid.NewGuid():N}{extension}";
            string storageKey = $"{referralId}/{fileName}";

            if (this.UseBlobStorage())
            {
                BlobContainerClient container = this.GetBlobContainer();
                await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
                BlobClient blobClient = container.GetBlobClient(storageKey);
                await using Stream uploadStream = file.OpenReadStream();
                await blobClient.UploadAsync(
                    uploadStream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = file.ContentType,
                            CacheControl = "public, max-age=300",
                        },
                    });
                return storageKey;
            }

            string directory = Path.Combine(this.webHostEnvironment.WebRootPath, LocalUploadFolder, referralId.ToString());
            Directory.CreateDirectory(directory);
            string physicalPath = Path.Combine(directory, fileName);
            await using (FileStream output = File.Create(physicalPath))
            {
                await file.CopyToAsync(output);
            }

            return $"{LocalUploadFolder}/{referralId}/{fileName}".Replace('\\', '/');
        }

        /// <summary>
        /// Deletes a stored referral image when present.
        /// </summary>
        /// <param name="imageUrl">Stored image path key.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            if (this.UseBlobStorage() && !imageUrl.StartsWith(LocalUploadFolder, StringComparison.OrdinalIgnoreCase))
            {
                BlobContainerClient container = this.GetBlobContainer();
                BlobClient blobClient = container.GetBlobClient(imageUrl);
                await blobClient.DeleteIfExistsAsync();
                return;
            }

            string relativePath = imageUrl.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
            string physicalPath = Path.Combine(this.webHostEnvironment.WebRootPath, relativePath);
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        /// <summary>
        /// Resolves a stored image path to a browser-ready URL.
        /// </summary>
        /// <param name="imageUrl">Stored image path key.</param>
        /// <returns>Public URL or null when no image is configured.</returns>
        public string ResolveUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri absoluteUri)
                && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                return absoluteUri.ToString();
            }

            if (this.UseBlobStorage() && !imageUrl.StartsWith(LocalUploadFolder, StringComparison.OrdinalIgnoreCase))
            {
                BlobContainerClient container = this.GetBlobContainer();
                return container.GetBlobClient(imageUrl).Uri.ToString();
            }

            return "/" + imageUrl.TrimStart('~', '/').Replace('\\', '/');
        }

        private bool UseBlobStorage()
        {
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            return !string.IsNullOrWhiteSpace(connectionString)
                && !connectionString.Contains("#{", StringComparison.Ordinal);
        }

        private BlobContainerClient GetBlobContainer()
        {
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            string containerName = this.configuration["AzureStorage:ReferralImagesContainerName"] ?? DefaultContainerName;
            return new BlobContainerClient(connectionString, containerName);
        }
    }
}
