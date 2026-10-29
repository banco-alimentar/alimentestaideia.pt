// -----------------------------------------------------------------------
// <copyright file="IFiscalDocumentCancellationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Cancels an issued original fiscal document while retaining its immutable evidence.
    /// </summary>
    public interface IFiscalDocumentCancellationService
    {
        /// <summary>
        /// Cancels an issued original document using the current trusted tenant and actor.
        /// </summary>
        /// <param name="request">The document and statutory motive.</param>
        /// <returns>The cancelled fiscal document.</returns>
        FiscalDocument Cancel(FiscalDocumentCancellationRequest request);
    }
}
