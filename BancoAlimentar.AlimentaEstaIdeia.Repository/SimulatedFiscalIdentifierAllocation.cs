// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalIdentifierAllocation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Represents a synthetically allocated receipt identifier.
    /// </summary>
    public sealed class SimulatedFiscalIdentifierAllocation
    {
        /// <summary>Gets or sets the sequence reserved within the simulation scope.</summary>
        public int Sequence { get; init; }

        /// <summary>Gets or sets the non-fiscal identifier for display in a simulation.</summary>
        public string SyntheticIdentifier { get; init; }
    }
}
