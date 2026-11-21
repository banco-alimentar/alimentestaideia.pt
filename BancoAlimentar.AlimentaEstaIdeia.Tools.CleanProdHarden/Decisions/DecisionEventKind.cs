// -----------------------------------------------------------------------
// <copyright file="DecisionEventKind.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    /// <summary>
    /// Kind of decision history event.
    /// </summary>
    public enum DecisionEventKind
    {
        /// <summary>
        /// The decision was saved.
        /// </summary>
        Saved,

        /// <summary>
        /// The decision was deferred.
        /// </summary>
        Deferred,

        /// <summary>
        /// A dry run ran.
        /// </summary>
        DryRun,

        /// <summary>
        /// An apply ran.
        /// </summary>
        Apply,
    }
}
