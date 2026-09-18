// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentSeries.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stores an AT-communicated fiscal document series and its concurrency token.
    /// </summary>
    public class FiscalDocumentSeries
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the issuer profile identifier.
        /// </summary>
        public int FiscalIssuerProfileId { get; set; }

        /// <summary>
        /// Gets or sets the issuer profile.
        /// </summary>
        public FiscalIssuerProfile FiscalIssuerProfile { get; set; }

        /// <summary>
        /// Gets or sets the code assigned to this document series.
        /// </summary>
        public string SeriesCode { get; set; }

        /// <summary>
        /// Gets or sets the SAF-T document type for this series.
        /// </summary>
        public string SaftDocumentType { get; set; }

        /// <summary>
        /// Gets or sets the processing method communicated to AT.
        /// </summary>
        public FiscalProcessingMethod ProcessingMethod { get; set; }

        /// <summary>
        /// Gets or sets the first permitted sequence number.
        /// </summary>
        public int FirstSequence { get; set; }

        /// <summary>
        /// Gets or sets the next sequence that can be reserved atomically.
        /// </summary>
        public int NextSequence { get; set; }

        /// <summary>
        /// Gets or sets the optional last permitted sequence number.
        /// </summary>
        public int? LastSequence { get; set; }

        /// <summary>
        /// Gets or sets the AT validation code used to derive ATCUD values.
        /// </summary>
        public string AtcudValidationCode { get; set; }

        /// <summary>
        /// Gets or sets the lifecycle state of the series.
        /// </summary>
        public FiscalSeriesStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the UTC instant on which the series starts.
        /// </summary>
        public DateTimeOffset StartsAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC instant on which the series closed.
        /// </summary>
        public DateTimeOffset? ClosedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the evidence reference for the AT series communication.
        /// </summary>
        public string AtCommunicationReference { get; set; }

        /// <summary>
        /// Gets or sets the row version used to reserve sequences safely.
        /// </summary>
        public byte[] RowVersion { get; set; }

        /// <summary>
        /// Gets or sets the documents issued in this series.
        /// </summary>
        public ICollection<FiscalDocument> Documents { get; set; }
    }
}
