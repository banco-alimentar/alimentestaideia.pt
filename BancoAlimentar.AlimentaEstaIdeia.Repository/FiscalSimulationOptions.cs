// -----------------------------------------------------------------------
// <copyright file="FiscalSimulationOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Contains process-local configuration for the development fiscal simulation.
    /// </summary>
    public sealed class FiscalSimulationOptions
    {
        /// <summary>
        /// Gets the configuration section containing the simulation setting.
        /// </summary>
        public const string SectionName = "Fiscal:Simulation";

        /// <summary>
        /// Gets or sets a value indicating whether the development-only simulation is enabled.
        /// The default is disabled so that an absent configuration cannot enable simulation.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
