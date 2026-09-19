// -----------------------------------------------------------------------
// <copyright file="IFiscalDocumentIssuanceService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

    /// <summary>
    /// Issues immutable fiscal evidence for an already paid persisted donation.
    /// </summary>
    public interface IFiscalDocumentIssuanceService
    {
        /// <summary>
        /// Issues the original fiscal document for a paid donation, idempotently.
        /// </summary>
        /// <param name="tenant">The trusted current tenant.</param>
        /// <param name="donation">The paid persisted donation.</param>
        /// <returns>The final issued fiscal document.</returns>
        FiscalDocument IssueOriginal(Tenant tenant, Donation donation);
    }
}
