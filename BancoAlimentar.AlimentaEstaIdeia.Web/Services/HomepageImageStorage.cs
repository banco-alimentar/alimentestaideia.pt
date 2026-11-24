// -----------------------------------------------------------------------
// <copyright file="HomepageImageStorage.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Stores immutable homepage image versions in persistent server storage.
    /// </summary>
    public class HomepageImageStorage
    {
        private const long MaximumImageSizeBytes = 10 * 1024 * 1024;
        private readonly string storagePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomepageImageStorage"/> class.
        /// </summary>
        /// <param name="environment">Web hosting environment.</param>
        /// <param name="configuration">Application configuration.</param>
        public HomepageImageStorage(IWebHostEnvironment environment, IConfiguration configuration)
        {
            string configuredPath = configuration["HomepageImages:StoragePath"];
            string homePath = Environment.GetEnvironmentVariable("HOME");
            this.storagePath = !string.IsNullOrWhiteSpace(configuredPath)
                ? Path.GetFullPath(configuredPath, environment.ContentRootPath)
                : !string.IsNullOrWhiteSpace(homePath)
                    ? Path.Combine(homePath, "data", "homepage-images")
                    : Path.Combine(environment.ContentRootPath, "App_Data", "homepage-images");
        }

        /// <summary>
        /// Gets the full path for a validated stored image filename.
        /// </summary>
        /// <param name="fileName">Generated server-side file name.</param>
        /// <returns>Image file path.</returns>
        public string GetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
            {
                throw new ArgumentException("Invalid image file name.", nameof(fileName));
            }

            return Path.Combine(this.storagePath, fileName);
        }

        /// <summary>
        /// Stores an uploaded PNG, JPEG, or WebP image under a generated filename.
        /// </summary>
        /// <param name="file">Uploaded file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Stored image metadata.</returns>
        public async Task<StoredHomepageImage> StoreAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0 || file.Length > MaximumImageSizeBytes)
            {
                throw new InvalidDataException("The image must be between 1 byte and 10 MB.");
            }

            byte[] header = new byte[12];
            await using (Stream input = file.OpenReadStream())
            {
                int bytesRead = 0;
                while (bytesRead < header.Length)
                {
                    int readCount = await input.ReadAsync(header.AsMemory(bytesRead), cancellationToken);
                    if (readCount == 0)
                    {
                        break;
                    }

                    bytesRead += readCount;
                }

                (string ContentType, string Extension)? imageType = GetImageType(header, bytesRead);
                if (imageType == null)
                {
                    throw new InvalidDataException("Upload a PNG, JPEG, or WebP image.");
                }

                string fileName = $"{Guid.NewGuid():N}{imageType.Value.Extension}";
                Directory.CreateDirectory(this.storagePath);
                string path = this.GetPath(fileName);
                input.Position = 0;
                await using FileStream output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await input.CopyToAsync(output, cancellationToken);
                return new StoredHomepageImage(fileName, imageType.Value.ContentType);
            }
        }

        private static (string ContentType, string Extension)? GetImageType(byte[] header, int bytesRead)
        {
            if (bytesRead >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            {
                return ("image/png", ".png");
            }

            if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return ("image/jpeg", ".jpg");
            }

            if (bytesRead >= 12
                && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
            {
                return ("image/webp", ".webp");
            }

            return null;
        }
    }
}
