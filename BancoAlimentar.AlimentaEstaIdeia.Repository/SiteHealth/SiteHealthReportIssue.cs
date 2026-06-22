// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportIssue.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System.Collections.Generic;

    /// <summary>
    /// A single monitored signal in the site health report.
    /// </summary>
    public class SiteHealthReportIssue
    {
        /// <summary>
        /// Gets or sets the stable issue code (event name or event:reason).
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the display title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        public SiteHealthReportSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the event count in the reporting window.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Gets or sets the estimated distinct incidents (when applicable).
        /// </summary>
        public long? DistinctIncidents { get; set; }

        /// <summary>
        /// Gets or sets the impact overview for webmasters.
        /// </summary>
        public string ImpactOverview { get; set; }

        /// <summary>
        /// Gets or sets optional recommended action.
        /// </summary>
        public string RecommendedAction { get; set; }

        /// <summary>
        /// Gets or sets optional breakdown lines (e.g. rejection reasons).
        /// </summary>
        public IList<string> DetailLines { get; set; } = new List<string>();
    }
}
