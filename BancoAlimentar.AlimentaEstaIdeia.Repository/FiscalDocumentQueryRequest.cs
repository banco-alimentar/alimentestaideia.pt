// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentQueryRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Contains the non-forgeable, user-selectable filters for fiscal document consultation.
    /// </summary>
    public class FiscalDocumentQueryRequest
    {
        /// <summary>Gets or sets the issuer profile selected for the query.</summary>
        public int FiscalIssuerProfileId { get; set; }

        /// <summary>Gets or sets the optional fiscal document series identifier.</summary>
        public int? FiscalDocumentSeriesId { get; set; }

        /// <summary>Gets or sets the optional document number.</summary>
        public string DocumentNumber { get; set; }

        /// <summary>Gets or sets the optional ATCUD.</summary>
        public string Atcud { get; set; }

        /// <summary>Gets or sets the optional donation identifier.</summary>
        public int? DonationId { get; set; }

        /// <summary>Gets or sets the optional inclusive start date.</summary>
        public DateOnly? StartDate { get; set; }

        /// <summary>Gets or sets the optional inclusive end date.</summary>
        public DateOnly? EndDate { get; set; }

        /// <summary>Gets or sets the optional fiscal document status.</summary>
        public FiscalDocumentStatus? Status { get; set; }
    }
}
