// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportActivity.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;

    /// <summary>A bounded, safe activity in an execution report.</summary>
    public sealed class FunctionExecutionReportActivity
    {
        /// <summary>Gets or sets the activity order.</summary>
        public int Ordinal { get; set; }

        /// <summary>Gets or sets the UTC timestamp.</summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>Gets or sets the stable activity key.</summary>
        public string ActivityKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the severity.</summary>
        public FunctionExecutionReportActivitySeverity Severity { get; set; }

        /// <summary>Gets or sets the optional scope.</summary>
        public string ScopeKey { get; set; }

        /// <summary>Gets or sets the safe human-readable message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Gets or sets bounded numeric counters.</summary>
        public Dictionary<string, long> Counters { get; set; } = new Dictionary<string, long>(StringComparer.Ordinal);
    }
}
