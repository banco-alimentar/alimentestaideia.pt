// -----------------------------------------------------------------------
// <copyright file="DecisionStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    /// <summary>
    /// Review state of a group decision.
    /// </summary>
    public enum DecisionStatus
    {
        /// <summary>
        /// Saved but not tested.
        /// </summary>
        Draft,

        /// <summary>
        /// Postponed by the reviewer.
        /// </summary>
        Deferred,

        /// <summary>
        /// The last dry run of this exact decision passed.
        /// </summary>
        DryRunPassed,

        /// <summary>
        /// The last dry run failed.
        /// </summary>
        DryRunFailed,

        /// <summary>
        /// Committed to the target database.
        /// </summary>
        Applied,

        /// <summary>
        /// The apply failed and was rolled back.
        /// </summary>
        ApplyFailed,
    }
}
