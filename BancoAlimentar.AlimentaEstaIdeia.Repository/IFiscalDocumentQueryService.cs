// -----------------------------------------------------------------------
// <copyright file="IFiscalDocumentQueryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System.Collections.Generic;

    /// <summary>
    /// Reads fiscal-document summaries within the tenant and issuer resolved from trusted context.
    /// </summary>
    public interface IFiscalDocumentQueryService
    {
        /// <summary>
        /// Queries immutable fiscal-document summaries using user-selectable, non-tenant filters.
        /// </summary>
        /// <param name="request">The selected issuer and optional document filters.</param>
        /// <returns>The matching fiscal-document summaries.</returns>
        IReadOnlyList<FiscalDocumentQueryResult> Query(FiscalDocumentQueryRequest request);
    }
}
