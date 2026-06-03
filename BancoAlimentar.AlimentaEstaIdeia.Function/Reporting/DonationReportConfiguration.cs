// -----------------------------------------------------------------------
// <copyright file="DonationReportConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Reporting
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Reads donation report publishing settings from configuration.
    /// </summary>
    public static class DonationReportConfiguration
    {
        /// <summary>
        /// Loads report options for the current tenant configuration.
        /// </summary>
        /// <param name="configuration">Tenant configuration.</param>
        /// <returns>Resolved options.</returns>
        public static DonationReportOptions ReadOptions(IConfiguration configuration)
        {
            string blobContainerName = configuration["DonationReport:BlobContainerName"]
                ?? configuration["Tenant:NormalizedName"];

            return new DonationReportOptions
            {
                Enabled = bool.TryParse(configuration["DonationReport:Enabled"], out bool enabled) && enabled,
                BlobContainerName = blobContainerName,
                BlobPrefix = configuration["DonationReport:BlobPrefix"] ?? DonationReportPaths.BlobPrefix,
                SiteTitle = configuration["DonationReport:SiteTitle"] ?? "Alimente esta ideia — Relatório",
            };
        }
    }
}
