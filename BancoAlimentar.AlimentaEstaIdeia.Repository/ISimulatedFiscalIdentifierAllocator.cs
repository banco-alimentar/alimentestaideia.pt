// -----------------------------------------------------------------------
// <copyright file="ISimulatedFiscalIdentifierAllocator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Reserves a sequence from a development-only simulated receipt counter.
    /// </summary>
    public interface ISimulatedFiscalIdentifierAllocator
    {
        /// <summary>
        /// Allocates the next synthetic identifier in one scope. The caller persists this work
        /// inside the same transaction as the simulated document.
        /// </summary>
        /// <param name="scope">The trusted simulation scope.</param>
        /// <returns>The allocated sequence and synthetic identifier.</returns>
        SimulatedFiscalIdentifierAllocation Allocate(SimulatedFiscalScope scope);
    }
}
