// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalSequence.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Stores a development-only counter for one simulated receipt scope.
    /// </summary>
    public class SimulatedFiscalSequence
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the synthetic tenant or food-bank scope that owns this counter.
        /// </summary>
        public string ScopeKey { get; set; }

        /// <summary>
        /// Gets or sets the invoicing strategy captured for this scope.
        /// </summary>
        public InvoicingStrategy InvoicingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the food-bank boundary for a multi-table scope.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the next synthetic sequence that can be allocated.
        /// </summary>
        public int NextSequence { get; set; }

        /// <summary>
        /// Gets or sets the optimistic concurrency token used for synthetic sequence allocation.
        /// </summary>
        public byte[] RowVersion { get; set; }
    }
}
