// -----------------------------------------------------------------------
// <copyright file="ISimulatedFiscalDocumentIssuanceService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

    /// <summary>
    /// Issues and reuses development-only simulated receipt evidence.
    /// </summary>
    public interface ISimulatedFiscalDocumentIssuanceService
    {
        /// <summary>
        /// Creates or returns the simulation for a paid donation in the trusted tenant.
        /// </summary>
        /// <param name="tenant">The trusted tenant selected by server-side middleware.</param>
        /// <param name="donation">The persisted paid donation.</param>
        /// <returns>The immutable simulated receipt record.</returns>
        SimulatedFiscalDocument IssueOrGet(Tenant tenant, Donation donation);
    }
}
