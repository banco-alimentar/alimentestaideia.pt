// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentCorrectionRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Identifies one idempotent correction requested for an issued original fiscal document.
    /// </summary>
    public class FiscalDocumentCorrectionRequest
    {
        /// <summary>
        /// Gets or sets the issued original fiscal document identifier.
        /// </summary>
        public int OriginalFiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the durable idempotency key for this correction.
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// Gets or sets the statutory correction motive.
        /// </summary>
        public string Motive { get; set; }
    }
}
