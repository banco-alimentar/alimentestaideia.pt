// -----------------------------------------------------------------------
// <copyright file="FiscalAuditEventRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Identifies a fiscal operation for append-only auditing without secret or personal payloads.
    /// </summary>
    public class FiscalAuditEventRequest
    {
        /// <summary>Gets or sets the bounded operation name.</summary>
        public string Action { get; set; }

        /// <summary>Gets or sets the bounded operation result.</summary>
        public string Result { get; set; }

        /// <summary>Gets or sets the optional issuer profile identifier.</summary>
        public int? FiscalIssuerProfileId { get; set; }

        /// <summary>Gets or sets the optional fiscal document series identifier.</summary>
        public int? FiscalDocumentSeriesId { get; set; }

        /// <summary>Gets or sets the optional fiscal document identifier.</summary>
        public int? FiscalDocumentId { get; set; }

        /// <summary>Gets or sets the optional request correlation identifier.</summary>
        public string CorrelationId { get; set; }
    }
}
