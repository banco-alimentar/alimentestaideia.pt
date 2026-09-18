// -----------------------------------------------------------------------
// <copyright file="FiscalIssuerProfile.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stores the versioned fiscal identity of a legal issuer.
    /// </summary>
    public class FiscalIssuerProfile
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the scope in which this issuer can issue fiscal documents.
        /// </summary>
        public FiscalScopeKind ScopeKind { get; set; }

        /// <summary>
        /// Gets or sets the optional food bank boundary for this issuer.
        /// </summary>
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the food bank boundary for this issuer.
        /// </summary>
        public FoodBank FoodBank { get; set; }

        /// <summary>
        /// Gets or sets the legal name of the issuer.
        /// </summary>
        public string LegalName { get; set; }

        /// <summary>
        /// Gets or sets the legal address captured on fiscal documents.
        /// </summary>
        public string LegalAddress { get; set; }

        /// <summary>
        /// Gets or sets the issuer's Portuguese tax registration number.
        /// </summary>
        public string TaxRegistrationNumber { get; set; }

        /// <summary>
        /// Gets or sets the SAF-T document type.
        /// </summary>
        public string SaftDocumentType { get; set; }

        /// <summary>
        /// Gets or sets the VAT or exemption rule approved for the issuer.
        /// </summary>
        public string TaxRule { get; set; }

        /// <summary>
        /// Gets or sets the software certificate number applied by this profile.
        /// </summary>
        public string SoftwareCertificateNumber { get; set; }

        /// <summary>
        /// Gets or sets the non-secret identifier of the signing key.
        /// </summary>
        public string SigningKeyIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the non-secret version of the signing key.
        /// </summary>
        public string SigningKeyVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the profile can be used for issuance.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the inclusive UTC instant from which the profile is valid.
        /// </summary>
        public DateTimeOffset ValidFromUtc { get; set; }

        /// <summary>
        /// Gets or sets the exclusive UTC instant at which the profile expires.
        /// </summary>
        public DateTimeOffset? ValidToUtc { get; set; }

        /// <summary>
        /// Gets or sets the series configured for this issuer.
        /// </summary>
        public ICollection<FiscalDocumentSeries> Series { get; set; }
    }
}
