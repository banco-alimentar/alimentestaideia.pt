// -----------------------------------------------------------------------
// <copyright file="ISimulatedReceiptQrPayloadBuilder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Builds the versioned private QR payload used exclusively by simulation receipts.
    /// </summary>
    public interface ISimulatedReceiptQrPayloadBuilder
    {
        /// <summary>
        /// Builds a non-fiscal QR payload for a persisted simulation document.
        /// </summary>
        /// <param name="document">The simulated document.</param>
        /// <returns>A private, versioned payload rather than an AT QR payload.</returns>
        string Build(SimulatedFiscalDocument document);
    }
}
