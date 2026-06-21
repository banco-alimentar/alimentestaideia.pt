// -----------------------------------------------------------------------
// <copyright file="ReferralQrCodeService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using QRCoder;

    /// <summary>
    /// Generates QR codes for referral campaign links.
    /// </summary>
    public class ReferralQrCodeService
    {
        /// <summary>
        /// Creates PNG bytes for the given URL.
        /// </summary>
        /// <param name="url">The URL to encode.</param>
        /// <returns>PNG image bytes.</returns>
        public byte[] CreatePngBytes(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Array.Empty<byte>();
            }

            using var generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(data);
            return pngQrCode.GetGraphic(5);
        }

        /// <summary>
        /// Creates a PNG data URI for the given URL.
        /// </summary>
        /// <param name="url">The URL to encode.</param>
        /// <returns>A data URI suitable for an img src attribute.</returns>
        public string CreateDataUri(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            byte[] pngBytes = this.CreatePngBytes(url);
            return "data:image/png;base64," + Convert.ToBase64String(pngBytes);
        }
    }
}
