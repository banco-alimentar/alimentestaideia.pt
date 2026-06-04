// -----------------------------------------------------------------------
// <copyright file="InvoiceDownloadTokenService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Data-protection-backed signed tokens for anonymous invoice downloads.
    /// </summary>
    public class InvoiceDownloadTokenService : IInvoiceDownloadTokenService
    {
        private const string ProtectorPurpose = "BancoAlimentar.AlimentaEstaIdeia.InvoiceDownload.v1";
        private const string DownloadPath = "/Identity/Account/Manage/GenerateInvoice";

        private readonly IDataProtector protector;
        private readonly InvoiceDownloadOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceDownloadTokenService"/> class.
        /// </summary>
        /// <param name="dataProtectionProvider">Data protection provider.</param>
        /// <param name="options">Invoice download options.</param>
        public InvoiceDownloadTokenService(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<InvoiceDownloadOptions> options)
        {
            this.protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
            this.options = options.Value;
        }

        /// <inheritdoc />
        public string CreateToken(Guid publicDonationId)
        {
            long expiresUnix = DateTimeOffset.UtcNow
                .AddHours(this.options.TokenLifetimeHours)
                .ToUnixTimeSeconds();
            string payload = $"{publicDonationId:N}|{expiresUnix}";
            return this.protector.Protect(payload);
        }

        /// <inheritdoc />
        public bool TryValidateToken(string token, out Guid publicDonationId)
        {
            publicDonationId = default;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string payload;
            try
            {
                payload = this.protector.Unprotect(token);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return false;
            }

            string[] parts = payload.Split('|');
            if (parts.Length != 2
                || !Guid.TryParse(parts[0], out publicDonationId)
                || !long.TryParse(parts[1], out long expiresUnix))
            {
                publicDonationId = default;
                return false;
            }

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnix)
            {
                publicDonationId = default;
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public string BuildDownloadUrl(HttpRequest request, Guid publicDonationId)
        {
            string relativeUrl = this.BuildRelativeDownloadUrl(publicDonationId);
            return $"{request.Scheme}://{request.Host}{relativeUrl}";
        }

        /// <inheritdoc />
        public string BuildRelativeDownloadUrl(Guid publicDonationId)
        {
            string token = this.CreateToken(publicDonationId);
            return $"{DownloadPath}?token={Uri.EscapeDataString(token)}";
        }
    }
}
