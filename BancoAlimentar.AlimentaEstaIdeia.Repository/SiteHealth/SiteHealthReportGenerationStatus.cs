// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportGenerationStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;

    /// <summary>
    /// Progress of a background site health report generation.
    /// </summary>
    public class SiteHealthReportGenerationStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether generation is running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets progress from 0 to 100.
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Gets or sets the current step description.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets when the run started (UTC).
        /// </summary>
        public DateTime? StartedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the run finished (UTC).
        /// </summary>
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the last error message, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the generated report timestamp after success.
        /// </summary>
        public DateTime? ReportGeneratedAtUtc { get; set; }
    }
}
