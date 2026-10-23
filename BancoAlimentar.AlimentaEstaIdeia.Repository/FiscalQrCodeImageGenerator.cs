// -----------------------------------------------------------------------
// <copyright file="FiscalQrCodeImageGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using QRCoder;

    /// <summary>
    /// Rasterises final Portuguese fiscal QR payloads into deterministic PNG images.
    /// </summary>
    public class FiscalQrCodeImageGenerator : IFiscalQrCodeImageGenerator
    {
        private static readonly string[] FieldPrefixes =
        {
            "A:",
            "B:",
            "C:",
            "D:",
            "E:",
            "F:",
            "G:",
            "H:",
            "I1:",
            "I7:",
            "I8:",
            "N:",
            "O:",
            "Q:",
            "R:",
        };

        /// <inheritdoc />
        public byte[] GeneratePng(string payload)
        {
            ValidatePayload(payload);

            using var qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var pngByteQrCode = new PngByteQRCode(qrCodeData);
            return pngByteQrCode.GetGraphic(10);
        }

        private static void ValidatePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new InvalidOperationException("QR image generation requires a fiscal payload.");
            }

            string[] fields = payload.Split('*');
            if (fields.Length != FieldPrefixes.Length)
            {
                throw new InvalidOperationException("QR image generation requires a complete fiscal payload.");
            }

            for (int index = 0; index < FieldPrefixes.Length; index++)
            {
                string prefix = FieldPrefixes[index];
                string field = fields[index];
                if (!field.StartsWith(prefix, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(field.Substring(prefix.Length)))
                {
                    throw new InvalidOperationException("QR image generation requires a valid fiscal payload.");
                }
            }

            if (!DateOnly.TryParseExact(
                    fields[5].Substring(2),
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                throw new InvalidOperationException("QR image generation requires a valid fiscal invoice date.");
            }

            ValidateAmount(fields[9], "I7:");
            ValidateAmount(fields[10], "I8:");
            ValidateAmount(fields[11], "N:");
            ValidateAmount(fields[12], "O:");

            if (fields[13].Length != 6)
            {
                throw new InvalidOperationException("QR image generation requires a valid fiscal hash excerpt.");
            }
        }

        private static void ValidateAmount(string field, string prefix)
        {
            string amount = field.Substring(prefix.Length);
            if (!decimal.TryParse(
                    amount,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal value) ||
                !string.Equals(
                    amount,
                    value.ToString("0.00", CultureInfo.InvariantCulture),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("QR image generation requires valid fiscal amounts.");
            }
        }
    }
}
