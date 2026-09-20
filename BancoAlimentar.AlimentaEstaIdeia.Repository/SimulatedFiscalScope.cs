// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalScope.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Identifies the isolated scope for a development-only receipt simulation.
    /// </summary>
    public sealed class SimulatedFiscalScope
    {
        /// <summary>Gets or sets the durable synthetic scope key.</summary>
        public string ScopeKey { get; init; }

        /// <summary>Gets or sets the strategy captured for this scope.</summary>
        public InvoicingStrategy InvoicingStrategy { get; init; }

        /// <summary>Gets or sets the food-bank boundary when the strategy requires one.</summary>
        public int? FoodBankId { get; init; }
    }
}
