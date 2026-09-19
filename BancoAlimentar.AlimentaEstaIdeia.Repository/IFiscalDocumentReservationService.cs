// -----------------------------------------------------------------------
// <copyright file="IFiscalDocumentReservationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Reserves an original fiscal document number without generating its final fiscal artefacts.
    /// </summary>
    public interface IFiscalDocumentReservationService
    {
        /// <summary>
        /// Reserves the next number in an eligible fiscal series for an original document.
        /// </summary>
        /// <param name="request">The donation, series and idempotency key for the reservation.</param>
        /// <returns>The existing or newly reserved fiscal document.</returns>
        FiscalDocument ReserveOriginal(FiscalDocumentReservationRequest request);
    }

    /// <summary>
    /// Identifies one idempotent reservation of an original fiscal document.
    /// </summary>
    public class FiscalDocumentReservationRequest
    {
        /// <summary>
        /// Gets or sets the persisted donation identifier.
        /// </summary>
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the fiscal series identifier.
        /// </summary>
        public int SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the durable caller-supplied idempotency key.
        /// </summary>
        public string IdempotencyKey { get; set; }
    }
}
