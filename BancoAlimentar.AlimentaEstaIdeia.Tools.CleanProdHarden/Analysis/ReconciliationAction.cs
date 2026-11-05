// -----------------------------------------------------------------------
// <copyright file="ReconciliationAction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Actions supported by the reconciliation executor. Names match the SQL plan contract.
    /// </summary>
    public enum ReconciliationAction
    {
        /// <summary>
        /// Keep the provider ID on one owner and clear it, audited, on every other member.
        /// </summary>
        ReconcileIdentifiers,

        /// <summary>
        /// Clear the provider ID, audited, on every member and keep all rows.
        /// </summary>
        RetireIdentifier,

        /// <summary>
        /// Archive every member row and delete the group. Requires safe deletion evidence and no dependents.
        /// </summary>
        DeleteGroup,
    }
}
