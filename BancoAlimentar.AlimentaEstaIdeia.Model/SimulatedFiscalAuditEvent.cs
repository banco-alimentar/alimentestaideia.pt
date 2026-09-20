// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalAuditEvent.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;

    /// <summary>
    /// Records append-only operational evidence for the development fiscal simulation.
    /// This entity is deliberately separate from fiscal audit records and has no AT or fiscal-document references.
    /// </summary>
    public class SimulatedFiscalAuditEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant that owns the simulated document.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets the optional food-bank scope used by a multi-table simulation.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the associated simulated receipt identifier.
        /// </summary>
        public int SimulatedFiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the associated simulated receipt.
        /// </summary>
        public SimulatedFiscalDocument SimulatedFiscalDocument { get; set; }

        /// <summary>
        /// Gets or sets the bounded operation name.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the bounded operation result.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the reason for this simulation event.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the immutable mode marker. This is always DevelopmentSimulation.
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets the authenticated actor when one is available.
        /// </summary>
        public string ActorId { get; set; }

        /// <summary>
        /// Gets or sets when the event occurred in UTC.
        /// </summary>
        public DateTimeOffset OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the request correlation identifier when one is available.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the optimistic concurrency token for append-only evidence.
        /// </summary>
        public byte[] RowVersion { get; set; }
    }
}
