// -----------------------------------------------------------------------
// <copyright file="DonationReportGenerationRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    /// <summary>
    /// Options for a single donation report generation run.
    /// </summary>
    public class DonationReportGenerationRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether to run even when DonationReport:Enabled is false.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Gets or sets an optional local directory for development output (e.g. Web wwwroot/report).
        /// </summary>
        public string LocalOutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets an optional blob container override (e.g. current tenant NormalizedName from HttpContext).
        /// </summary>
        public string BlobContainerNameOverride { get; set; }
    }
}
