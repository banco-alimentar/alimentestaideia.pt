// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentSeriesAdministrationRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

#pragma warning disable CS1591
#pragma warning disable SA1516
#pragma warning disable SA1600
    /// <summary>Contains the communicated, non-secret series configuration.</summary>
    public class FiscalDocumentSeriesAdministrationRequest
    {
        public int Id { get; set; }
        public int FiscalIssuerProfileId { get; set; }
        public string SeriesCode { get; set; }
        public string SaftDocumentType { get; set; }
        public FiscalProcessingMethod ProcessingMethod { get; set; }
        public int FirstSequence { get; set; }
        public int NextSequence { get; set; }
        public int? LastSequence { get; set; }
        public string AtcudValidationCode { get; set; }
        public string AtCommunicationReference { get; set; }
        public DateTimeOffset StartsAtUtc { get; set; }
        public FiscalSeriesStatus Status { get; set; }
    }
#pragma warning restore SA1600
#pragma warning restore SA1516
#pragma warning restore CS1591
}
