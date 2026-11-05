// -----------------------------------------------------------------------
// <copyright file="SuggestedAction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// The SQL planner's <c>SuggestedAction</c> value.
    /// </summary>
    public enum SuggestedAction
    {
        /// <summary>
        /// Keep the ID on the confirmed owner.
        /// </summary>
        ReconcileIdentifiers,

        /// <summary>
        /// Retire the ID on every member.
        /// </summary>
        RetireIdentifier,

        /// <summary>
        /// Several donations are confirmed by the group; duplicate donations need separate handling.
        /// </summary>
        ResolveDuplicateDonations,

        /// <summary>
        /// Manual review is required.
        /// </summary>
        Review,
    }
}
