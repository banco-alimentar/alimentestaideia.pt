// -----------------------------------------------------------------------
// <copyright file="ISimulatedFiscalAuditService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

    /// <summary>
    /// Appends development-simulation operational evidence without using fiscal audit entities.
    /// </summary>
    public interface ISimulatedFiscalAuditService
    {
        /// <summary>
        /// Appends an audit event for a trusted tenant and simulated receipt in the current unit of work.
        /// </summary>
        /// <param name="tenant">The tenant that owns the simulated receipt.</param>
        /// <param name="document">The issued simulated receipt.</param>
        /// <param name="request">The bounded simulation operation details.</param>
        void Append(Tenant tenant, SimulatedFiscalDocument document, SimulatedFiscalAuditEventRequest request);
    }
}
