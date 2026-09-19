// -----------------------------------------------------------------------
// <copyright file="ISaftExportService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Exports immutable fiscal documents in the applicable SAF-T (PT) format.
    /// </summary>
    public interface ISaftExportService
    {
        /// <summary>
        /// Exports the fiscal snapshots that belong to the requested issuer and date range.
        /// </summary>
        /// <param name="request">The trusted export boundary.</param>
        /// <returns>The deterministic SAF-T (PT) XML document.</returns>
        string Export(SaftExportRequest request);
    }
}
