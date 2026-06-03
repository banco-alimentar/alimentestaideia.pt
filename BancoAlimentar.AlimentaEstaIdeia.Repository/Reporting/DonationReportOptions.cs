// -----------------------------------------------------------------------
// <copyright file="DonationReportOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    /// <summary>
    /// Configuration for static donation report publishing.
    /// </summary>
    public class DonationReportOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether report generation is enabled for this tenant.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the tenant blob container name (defaults to tenant NormalizedName from configuration).
        /// </summary>
        public string BlobContainerName { get; set; }

        /// <summary>
        /// Gets or sets blob prefix inside the container (default <c>wwwroot/report/</c>).
        /// </summary>
        public string BlobPrefix { get; set; } = DonationReportPaths.BlobPrefix;

        /// <summary>
        /// Gets or sets the public site title.
        /// </summary>
        public string SiteTitle { get; set; } = "Alimente esta ideia — Relatório";
    }
}
