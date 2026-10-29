// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalAuditEventRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Describes a bounded development-simulation operation without caller-supplied identity fields.
    /// </summary>
    public class SimulatedFiscalAuditEventRequest
    {
        /// <summary>Gets or sets the bounded operation name.</summary>
        public string Action { get; set; }

        /// <summary>Gets or sets the bounded operation result.</summary>
        public string Result { get; set; }

        /// <summary>Gets or sets the bounded reason for the simulation operation.</summary>
        public string Reason { get; set; }
    }
}
