// -----------------------------------------------------------------------
// <copyright file="ISimulationChecksumService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Calculates a local integrity checksum for a simulated receipt.
    /// </summary>
    public interface ISimulationChecksumService
    {
        /// <summary>Calculates a checksum over the stable simulation receipt data.</summary>
        /// <param name="document">The simulated receipt.</param>
        /// <returns>A hexadecimal checksum.</returns>
        string Calculate(BancoAlimentar.AlimentaEstaIdeia.Model.SimulatedFiscalDocument document);
    }
}
