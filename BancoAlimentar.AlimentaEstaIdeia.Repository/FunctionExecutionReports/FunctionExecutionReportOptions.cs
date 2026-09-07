// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>Configuration for execution report persistence.</summary>
    public sealed class FunctionExecutionReportOptions
    {
        /// <summary>Gets or sets whether report persistence is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Gets or sets the dedicated storage connection string.</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Gets or sets the private blob container name.</summary>
        public string ContainerName { get; set; } = "function-execution-reports";

        /// <summary>Gets or sets the retention period in days.</summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>Gets or sets the activity limit.</summary>
        public int MaxActivities { get; set; } = 500;

        /// <summary>Gets or sets the maximum activity message length.</summary>
        public int MaxActivityMessageLength { get; set; } = 1000;

        /// <summary>Gets or sets the maximum number of warnings retained.</summary>
        public int MaxWarnings { get; set; } = 100;

        /// <summary>Gets or sets the maximum number of errors retained.</summary>
        public int MaxErrors { get; set; } = 100;

        /// <summary>Gets or sets the maximum number of summary counters retained.</summary>
        public int MaxCounters { get; set; } = 100;

        /// <summary>Gets or sets the prefix used by the externally managed Blob lifecycle policy.</summary>
        public string LifecyclePrefix { get; set; } = "v1/";

        /// <summary>Validates configuration values.</summary>
        /// <param name="options">Options to validate.</param>
        public static void Validate(FunctionExecutionReportOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.RetentionDays < 90)
            {
                throw new ArgumentOutOfRangeException(nameof(options.RetentionDays), "Execution report retention must be at least 90 days.");
            }

            if (options.MaxActivities < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxActivities));
            }

            if (options.MaxActivityMessageLength < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxActivityMessageLength));
            }

            if (options.MaxWarnings < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxWarnings));
            }

            if (options.MaxErrors < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxErrors));
            }

            if (options.MaxCounters < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxCounters));
            }

            if (string.IsNullOrWhiteSpace(options.LifecyclePrefix)
                || options.LifecyclePrefix.Contains("..", StringComparison.Ordinal)
                || !options.LifecyclePrefix.EndsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("The lifecycle prefix must be a non-traversing blob prefix ending with '/'.", nameof(options.LifecyclePrefix));
            }

            if (string.IsNullOrWhiteSpace(options.ContainerName)
                || options.ContainerName.Length > 63
                || options.ContainerName != options.ContainerName.ToLowerInvariant())
            {
                throw new ArgumentException("The execution report container name must be a lowercase Azure container name.", nameof(options.ContainerName));
            }
        }
    }
}
