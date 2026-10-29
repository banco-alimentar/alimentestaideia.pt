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
    /// Rasterizes only the private simulation QR payload format.
    /// </summary>
    public class SimulatedReceiptQrCodeImageGenerator : ISimulatedReceiptQrCodeImageGenerator
    {
        /// <inheritdoc />
        public byte[] GeneratePng(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith("AEI-SIM/v1;id=", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("QR image generation requires a private simulation payload.");
            }

            using var generator = new QRCodeGenerator();
            using QRCodeData qrCodeData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(qrCodeData).GetGraphic(10);
        }
    }
}
