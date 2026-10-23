// -----------------------------------------------------------------------
// <copyright file="IFiscalEnvironmentGate.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Applies the environment-specific policy that protects fiscal issuance.
    /// </summary>
    public interface IFiscalEnvironmentGate
    {
        /// <summary>
        /// Ensures that the configured issuer and series can issue a fiscal document in the
        /// current host environment.
        /// </summary>
        /// <param name="fiscalIssuerProfile">The issuer profile selected for issuance.</param>
        /// <param name="fiscalDocumentSeries">The series selected for issuance.</param>
        void EnsureIssuanceAllowed(
            FiscalIssuerProfile fiscalIssuerProfile,
            FiscalDocumentSeries fiscalDocumentSeries);
    }
}
