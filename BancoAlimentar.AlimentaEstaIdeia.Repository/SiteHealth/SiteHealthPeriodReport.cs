// -----------------------------------------------------------------------
// <copyright file="SiteHealthPeriodReport.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System.Collections.Generic;

    /// <summary>
    /// Health analysis for a single time window.
    /// </summary>
    public class SiteHealthPeriodReport
    {
        /// <summary>
        /// Gets or sets the window label (e.g. 24h, 7d).
        /// </summary>
        public string WindowLabel { get; set; }

        /// <summary>
        /// Gets or sets the overall status.
        /// </summary>
        public SiteHealthOverallStatus OverallStatus { get; set; }

        /// <summary>
        /// Gets or sets the summary sentence for webmasters.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the total HTTP request count (production role).
        /// </summary>
        public long RequestCount { get; set; }

        /// <summary>
        /// Gets or sets failed HTTP requests excluding 404.
        /// </summary>
        public long FailedRequestCount { get; set; }

        /// <summary>
        /// Gets or sets unhandled exception count (production role).
        /// </summary>
        public long ExceptionCount { get; set; }

        /// <summary>
        /// Gets or sets production issues ordered by severity.
        /// </summary>
        public IList<SiteHealthReportIssue> ProductionIssues { get; set; } = new List<SiteHealthReportIssue>();

        /// <summary>
        /// Gets or sets non-production or test noise (informational).
        /// </summary>
        public IList<SiteHealthReportIssue> InformationalIssues { get; set; } = new List<SiteHealthReportIssue>();
    }
}
