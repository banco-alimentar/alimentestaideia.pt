// -----------------------------------------------------------------------
// <copyright file="SimulatedReceiptQrCodeImageGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using QRCoder;

    /// <summary>
    /// Rasterises private development simulation QR payloads without accepting AT QR syntax.
    /// </summary>
    public class SimulatedReceiptQrCodeImageGenerator : ISimulatedReceiptQrCodeImageGenerator
    {
        /// <inheritdoc />
        public byte[] GeneratePng(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith("AEI-SIM/v1;", StringComparison.Ordinal))
            {
                throw new ArgumentException("A private AEI-SIM/v1 payload is required for a simulation QR image.", nameof(payload));
            }

            using var qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var pngByteQrCode = new PngByteQRCode(qrCodeData);
            return pngByteQrCode.GetGraphic(10);
        }
    }
}
