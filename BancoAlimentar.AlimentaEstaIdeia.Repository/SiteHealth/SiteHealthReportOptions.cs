// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    /// <summary>
    /// Configuration for site health reporting.
    /// </summary>
    public class SiteHealthReportOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "SiteHealthReport";

        /// <summary>
        /// Gets or sets a value indicating whether scheduled generation is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the Log Analytics workspace customer id (GUID).
        /// </summary>
        public string LogAnalyticsWorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets the blob container used to store report snapshots.
        /// </summary>
        public string BlobContainerName { get; set; } = "site-health";

        /// <summary>
        /// Gets or sets the production App Insights cloud role name filter.
        /// </summary>
        public string ProductionAppRoleName { get; set; } = "alimentaestaideia";

        /// <summary>
        /// Gets or sets the developer slot App Insights cloud role name.
        /// </summary>
        public string DeveloperAppRoleName { get; set; } = "alimentaestaideia-developer";
    }
}
