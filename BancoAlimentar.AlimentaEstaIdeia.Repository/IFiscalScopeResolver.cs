// -----------------------------------------------------------------------
// <copyright file="IFiscalScopeResolver.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Resolves the tenant and optional food bank boundary before fiscal issuance.
    /// </summary>
    public interface IFiscalScopeResolver
    {
        /// <summary>
        /// Resolves the fiscal scope that applies to a donation in the supplied tenant.
        /// </summary>
        /// <param name="tenant">The current tenant.</param>
        /// <param name="donation">The donation for which a fiscal document may be issued.</param>
        /// <returns>The resolved fiscal scope.</returns>
        FiscalScope Resolve(Tenant tenant, Donation donation);
    }

    /// <summary>
    /// Identifies the database boundary and issuer selected for a fiscal operation.
    /// </summary>
    public class FiscalScope
    {
        /// <summary>
        /// Gets or sets the current infrastructure tenant identifier.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets the invoicing strategy selected for the tenant.
        /// </summary>
        public InvoicingStrategy InvoicingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the optional food bank boundary for the operation.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the issuer profile selected for the operation.
        /// </summary>
        public FiscalIssuerProfile FiscalIssuerProfile { get; set; }

        /// <summary>
        /// Gets or sets the fiscal series selected for the operation.
        /// </summary>
        public FiscalDocumentSeries FiscalDocumentSeries { get; set; }
    }
}
