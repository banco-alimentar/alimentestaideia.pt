// -----------------------------------------------------------------------
// <copyright file="ISimulatedReceiptQrCodeImageGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Rasterises a private development simulation QR payload.
    /// </summary>
    public interface ISimulatedReceiptQrCodeImageGenerator
    {
        /// <summary>
        /// Generates a PNG representation of a private simulation payload.
        /// </summary>
        /// <param name="payload">The private simulation payload.</param>
        /// <returns>The PNG bytes.</returns>
        byte[] GeneratePng(string payload);
    }
}
