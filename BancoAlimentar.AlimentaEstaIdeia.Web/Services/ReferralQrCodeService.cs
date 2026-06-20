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

            using var generator = new QRCodeGenerator();
            using QRCodeData data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(data);
            byte[] pngBytes = pngQrCode.GetGraphic(5);
            return "data:image/png;base64," + Convert.ToBase64String(pngBytes);
        }
    }
}
