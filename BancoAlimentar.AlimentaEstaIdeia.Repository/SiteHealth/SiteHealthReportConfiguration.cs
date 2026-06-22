// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Reads site health report settings from configuration.
    /// </summary>
    public static class SiteHealthReportConfiguration
    {
        /// <summary>
        /// Default workspace id for alimentaestaideia-core (see Documentation/Application-Insights.md).
        /// </summary>
        public const string DefaultWorkspaceId = "41951796-bdfb-4289-9456-69f2e3d991b7";

        /// <summary>
        /// Loads options from configuration.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>Resolved options.</returns>
        public static SiteHealthReportOptions ReadOptions(IConfiguration configuration)
        {
            SiteHealthReportOptions options = new SiteHealthReportOptions();
            configuration.GetSection(SiteHealthReportOptions.SectionName).Bind(options);

            if (string.IsNullOrWhiteSpace(options.LogAnalyticsWorkspaceId))
            {
                options.LogAnalyticsWorkspaceId = DefaultWorkspaceId;
            }

            if (string.IsNullOrWhiteSpace(options.LogAnalyticsWorkspaceResourceId))
            {
                options.LogAnalyticsWorkspaceResourceId = SiteHealthApplicationInsightsLinkBuilder.DefaultWorkspaceResourceId;
            }

            return options;
        }
    }
}
