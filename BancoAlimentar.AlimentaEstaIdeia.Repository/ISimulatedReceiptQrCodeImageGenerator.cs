// -----------------------------------------------------------------------
// <copyright file="ISimulatedReceiptQrCodeImageGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Rasterizes private simulation QR payloads.
    /// </summary>
    public interface ISimulatedReceiptQrCodeImageGenerator
    {
        /// <summary>Generates a PNG for a private simulation payload.</summary>
        /// <param name="payload">The simulation payload.</param>
        /// <returns>PNG bytes.</returns>
        byte[] GeneratePng(string payload);
    }
}
