// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentCancellationRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Identifies an issued fiscal document and the reason for its cancellation.
    /// </summary>
    public class FiscalDocumentCancellationRequest
    {
        /// <summary>
        /// Gets or sets the immutable fiscal document identifier.
        /// </summary>
        public int FiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the statutory cancellation motive.
        /// </summary>
        public string Motive { get; set; }
    }
}
