// -----------------------------------------------------------------------
// <copyright file="SiteHealthReport.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Snapshot of site quality signals from Application Insights.
    /// </summary>
    public class SiteHealthReport
    {
        /// <summary>
        /// Gets or sets when the report was generated (UTC).
        /// </summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets who triggered generation (e.g. AzureFunction, AdminPage).
        /// </summary>
        public string GeneratedBy { get; set; }

        /// <summary>
        /// Gets or sets the Log Analytics workspace id queried.
        /// </summary>
        public string WorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets the production App Role name filter used.
        /// </summary>
        public string ProductionAppRoleName { get; set; }

        /// <summary>
        /// Gets or sets per-window analysis.
        /// </summary>
        public IList<SiteHealthPeriodReport> Periods { get; set; } = new List<SiteHealthPeriodReport>();
    }
}
