// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentEvent.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;

    /// <summary>
    /// Records an append-only lifecycle event for a fiscal document.
    /// </summary>
    public class FiscalDocumentEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the fiscal document identifier.
        /// </summary>
        public int FiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the fiscal document.
        /// </summary>
        public FiscalDocument FiscalDocument { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the actor responsible for the event.
        /// </summary>
        public string ActorId { get; set; }

        /// <summary>
        /// Gets or sets when the event occurred in UTC.
        /// </summary>
        public DateTimeOffset OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the non-personal reason for the event.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier of the request.
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
