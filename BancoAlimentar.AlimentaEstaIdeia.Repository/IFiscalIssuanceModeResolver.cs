// -----------------------------------------------------------------------
// <copyright file="IFiscalIssuanceModeResolver.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Selects the server-side fiscal issuance workflow from process configuration.
    /// </summary>
    public interface IFiscalIssuanceModeResolver
    {
        /// <summary>
        /// Resolves the only fiscal issuance mode allowed for the current process.
        /// </summary>
        /// <returns>The configured issuance mode.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when simulation is enabled outside the exact Development environment.
        /// </exception>
        FiscalIssuanceMode Resolve();
    }
}
