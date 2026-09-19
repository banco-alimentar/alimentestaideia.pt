// -----------------------------------------------------------------------
// <copyright file="FiscalIssuerProfileAdministrationRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// <summary>Contains only non-secret issuer profile configuration.</summary>
    public class FiscalIssuerProfileAdministrationRequest
    {
        public int Id { get; set; }
        public FiscalScopeKind ScopeKind { get; set; }
        public int? FoodBankId { get; set; }
        public string LegalName { get; set; }
        public string LegalAddress { get; set; }
        public string TaxRegistrationNumber { get; set; }
        public string SaftDocumentType { get; set; }
        public string TaxRule { get; set; }
        public string SoftwareCertificateNumber { get; set; }
        public string SigningKeyIdentifier { get; set; }
        public string SigningKeyVersion { get; set; }
        public DateTimeOffset ValidFromUtc { get; set; }
        public DateTimeOffset? ValidToUtc { get; set; }
        public bool IsActive { get; set; }
    }
#pragma warning restore SA1600
#pragma warning restore SA1516
#pragma warning restore CS1591
}
