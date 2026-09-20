// -----------------------------------------------------------------------
// <copyright file="IFiscalAuditService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Appends compact operational fiscal audit events from trusted request context.
    /// </summary>
    public interface IFiscalAuditService
    {
        /// <summary>
        /// Records a fiscal operational event without accepting caller-provided tenant or actor identities.
        /// </summary>
        /// <param name="request">The safe event identifiers and result.</param>
        /// <returns>The persisted append-only audit event.</returns>
        FiscalAuditEvent Record(FiscalAuditEventRequest request);
    }
}
