// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportSummary.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System.Collections.Generic;

    /// <summary>Common and function-specific execution counters.</summary>
    public sealed class FunctionExecutionReportSummary
    {
        /// <summary>Gets or sets the number of activities attempted.</summary>
        public int ActivitiesAttempted { get; set; }

        /// <summary>Gets or sets the number of activities completed.</summary>
        public int ActivitiesCompleted { get; set; }

        /// <summary>Gets or sets the number of warnings.</summary>
        public int Warnings { get; set; }

        /// <summary>Gets or sets the number of errors.</summary>
        public int Errors { get; set; }

        /// <summary>Gets or sets the number of changed records.</summary>
        public long RecordsChanged { get; set; }

        /// <summary>Gets or sets the number of tenant configurations discovered.</summary>
        public int TenantsDiscovered { get; set; }

        /// <summary>Gets or sets the number of tenants whose work was started.</summary>
        public int TenantsProcessed { get; set; }

        /// <summary>Gets or sets the number of tenants completed successfully.</summary>
        public int TenantsSucceeded { get; set; }

        /// <summary>Gets or sets the number of tenants that failed.</summary>
        public int TenantsFailed { get; set; }

        /// <summary>Gets or sets the number of tenants skipped.</summary>
        public int TenantsSkipped { get; set; }

        /// <summary>Gets or sets function-specific counters.</summary>
        public Dictionary<string, long> Counters { get; set; } = new Dictionary<string, long>();

        /// <summary>Gets or sets a safe final explanation.</summary>
        public string Message { get; set; }
    }
}
