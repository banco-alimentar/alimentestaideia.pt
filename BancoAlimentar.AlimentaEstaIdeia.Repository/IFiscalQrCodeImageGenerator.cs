// -----------------------------------------------------------------------
// <copyright file="IFiscalQrCodeImageGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Rasterises a final Portuguese fiscal QR payload.
    /// </summary>
    public interface IFiscalQrCodeImageGenerator
    {
        /// <summary>
        /// Generates a portable network graphics representation of the fiscal QR payload.
        /// </summary>
        /// <param name="payload">The final official fiscal QR payload.</param>
        /// <returns>The square PNG bytes.</returns>
        byte[] GeneratePng(string payload);
    }
}
