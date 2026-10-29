// -----------------------------------------------------------------------
// <copyright file="ISimulatedReceiptQrPayloadBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Builds a private QR payload for a simulated receipt.
    /// </summary>
    public interface ISimulatedReceiptQrPayloadBuilder
    {
        /// <summary>Builds a non-fiscal QR payload.</summary>
        /// <param name="document">The simulated receipt.</param>
        /// <returns>A private simulation payload.</returns>
        string Build(BancoAlimentar.AlimentaEstaIdeia.Model.SimulatedFiscalDocument document);
    }
}
