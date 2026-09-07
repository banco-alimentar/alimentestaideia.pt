// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportLifecyclePolicy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Describes the Azure Blob lifecycle rule that should be configured outside the application.
    /// The application deliberately does not delete report blobs during function execution.
    /// </summary>
    public sealed class FunctionExecutionReportLifecyclePolicy
    {
        /// <summary>Gets the stable Azure lifecycle rule name.</summary>
        public string Name { get; private set; }

        /// <summary>Gets the blob prefixes covered by the rule.</summary>
        public IReadOnlyList<string> PrefixMatch { get; private set; }

        /// <summary>Gets the retention threshold for Azure lifecycle management.</summary>
        public int DeleteAfterDaysSinceModificationGreaterThan { get; private set; }

        /// <summary>Creates a retention rule descriptor for Azure Storage management policy deployment.</summary>
        /// <param name="options">Validated report options.</param>
        /// <returns>A safe rule descriptor.</returns>
        public static FunctionExecutionReportLifecyclePolicy Create(FunctionExecutionReportOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            FunctionExecutionReportOptions.Validate(options);
            return new FunctionExecutionReportLifecyclePolicy
            {
                Name = "function-execution-reports-retention",
                PrefixMatch = new List<string> { options.LifecyclePrefix },
                DeleteAfterDaysSinceModificationGreaterThan = options.RetentionDays,
            };
        }
    }
}
