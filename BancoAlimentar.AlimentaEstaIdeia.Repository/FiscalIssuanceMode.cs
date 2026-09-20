// -----------------------------------------------------------------------
// <copyright file="FiscalIssuanceMode.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Identifies the server-selected receipt issuance workflow.
    /// </summary>
    public enum FiscalIssuanceMode
    {
        /// <summary>
        /// Uses the certified fiscal issuance workflow.
        /// </summary>
        Real = 0,

        /// <summary>
        /// Uses the development-only, non-fiscal simulation workflow.
        /// </summary>
        Simulation = 1,
    }
}
