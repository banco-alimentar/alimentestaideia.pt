// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReport.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;

    /// <summary>Versioned immutable execution report document.</summary>
    public sealed class FunctionExecutionReport
    {
        /// <summary>Gets or sets the schema version.</summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>Gets or sets the stable function key.</summary>
        public string FunctionKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the display/function name.</summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>Gets or sets the execution identifier.</summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>Gets or sets the invocation identifier.</summary>
        public string InvocationId { get; set; } = string.Empty;

        /// <summary>Gets or sets the safe Application Insights correlation identifier.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Gets or sets the trigger type.</summary>
        public string TriggerType { get; set; } = "Unknown";

        /// <summary>Gets or sets the environment name.</summary>
        public string Environment { get; set; }

        /// <summary>Gets or sets the deployment slot.</summary>
        public string SlotKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the tenant/global scope.</summary>
        public string ScopeKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the UTC start timestamp.</summary>
        public DateTime StartedAtUtc { get; set; }

        /// <summary>Gets or sets the UTC completion timestamp.</summary>
        public DateTime CompletedAtUtc { get; set; }

        /// <summary>Gets or sets the duration in milliseconds.</summary>
        public long DurationMilliseconds { get; set; }

        /// <summary>Gets or sets the final outcome.</summary>
        public FunctionExecutionReportOutcome Outcome { get; set; }

        /// <summary>Gets or sets whether business data was changed.</summary>
        public bool BusinessDataChanged { get; set; }

        /// <summary>Gets or sets the bounded activity list.</summary>
        public List<FunctionExecutionReportActivity> Activities { get; set; } = new List<FunctionExecutionReportActivity>();

        /// <summary>Gets or sets the final summary.</summary>
        public FunctionExecutionReportSummary Summary { get; set; } = new FunctionExecutionReportSummary();

        /// <summary>Gets or sets safe warnings.</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>Gets or sets safe errors.</summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
