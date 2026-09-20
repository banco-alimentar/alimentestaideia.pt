// -----------------------------------------------------------------------
// <copyright file="ISimulationChecksumService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Creates a local integrity checksum for simulated receipts.
    /// This is not an AT fiscal signature and never uses a fiscal signing key.
    /// </summary>
    public interface ISimulationChecksumService
    {
        /// <summary>
        /// Calculates a deterministic local checksum for a simulated document.
        /// </summary>
        /// <param name="document">The simulated document.</param>
        /// <returns>A non-fiscal local checksum.</returns>
        string Calculate(SimulatedFiscalDocument document);
    }
}
