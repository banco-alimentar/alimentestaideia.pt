// -----------------------------------------------------------------------
// <copyright file="FiscalAuditEvent.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;

    /// <summary>
    /// Records an append-only fiscal audit event without secret material.
    /// </summary>
    public class FiscalAuditEvent
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the application tenant identifier associated with the event.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets the optional food bank boundary associated with the event.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the optional issuer profile identifier associated with the event.
        /// </summary>
        public int? FiscalIssuerProfileId { get; set; }

        /// <summary>
        /// Gets or sets the issuer profile associated with the event.
        /// </summary>
        public FiscalIssuerProfile FiscalIssuerProfile { get; set; }

        /// <summary>
        /// Gets or sets the optional fiscal series identifier associated with the event.
        /// </summary>
        public int? FiscalDocumentSeriesId { get; set; }

        /// <summary>
        /// Gets or sets the fiscal series associated with the event.
        /// </summary>
        public FiscalDocumentSeries FiscalDocumentSeries { get; set; }

        /// <summary>
        /// Gets or sets the optional fiscal document identifier associated with the event.
        /// </summary>
        public int? FiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the fiscal document associated with the event.
        /// </summary>
        public FiscalDocument FiscalDocument { get; set; }

        /// <summary>
        /// Gets or sets the action being audited.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the result of the action.
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the actor responsible for the action.
        /// </summary>
        public string ActorId { get; set; }

        /// <summary>
        /// Gets or sets when the event occurred in UTC.
        /// </summary>
        public DateTimeOffset OccurredAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier of the request.
        /// </summary>
        public string CorrelationId { get; set; }
    }
}
