// -----------------------------------------------------------------------
// <copyright file="SaftExportRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

    /// <summary>
    /// Defines the trusted tenant, issuer and inclusive accounting period for a SAF-T export.
    /// </summary>
    public class SaftExportRequest
    {
        /// <summary>
        /// Gets or sets the trusted tenant for the current request.
        /// </summary>
        public Tenant Tenant { get; set; }

        /// <summary>
        /// Gets or sets the immutable issuer profile identifier to export.
        /// </summary>
        public int FiscalIssuerProfileId { get; set; }

        /// <summary>
        /// Gets or sets the first included invoice date.
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Gets or sets the last included invoice date.
        /// </summary>
        public DateOnly EndDate { get; set; }
    }
}
