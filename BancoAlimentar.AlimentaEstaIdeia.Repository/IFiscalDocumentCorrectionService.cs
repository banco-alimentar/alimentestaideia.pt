// -----------------------------------------------------------------------
// <copyright file="IFiscalDocumentCorrectionService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Creates an independently identified correction for an issued original fiscal document.
    /// </summary>
    public interface IFiscalDocumentCorrectionService
    {
        /// <summary>
        /// Creates or returns the idempotent correction for an issued original document.
        /// </summary>
        /// <param name="request">The original document, idempotency key and statutory motive.</param>
        /// <returns>The separately signed and immutable correction document.</returns>
        FiscalDocument CreateCorrection(FiscalDocumentCorrectionRequest request);
    }
}
