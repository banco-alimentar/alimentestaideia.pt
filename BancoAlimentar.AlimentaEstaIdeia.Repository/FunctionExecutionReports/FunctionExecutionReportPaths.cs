// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportPaths.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>Builds safe, tenant-scoped blob paths for execution reports.</summary>
    public static class FunctionExecutionReportPaths
    {
        private static readonly Regex SafeSegment = new Regex("^[a-z0-9][a-z0-9-_.]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Builds an immutable execution report path.</summary>
        public static string BuildExecutionPath(string slotKey, string scopeKey, string functionKey, DateTime completedAtUtc, string executionId)
        {
            string slot = NormalizeSegment(slotKey, nameof(slotKey));
            string scope = NormalizeSegment(scopeKey, nameof(scopeKey));
            string function = NormalizeSegment(functionKey, nameof(functionKey));
            string id = NormalizeSegment(executionId, nameof(executionId));
            DateTime utc = EnsureUtc(completedAtUtc);

            return string.Format(
                CultureInfo.InvariantCulture,
                "v1/{0}/{1}/{2}/executions/{3:yyyy}/{3:MM}/{3:dd}/{4}.json",
                slot,
                scope,
                function,
                utc,
                id);
        }

        /// <summary>Builds the latest pointer path.</summary>
        public static string BuildLatestPath(string slotKey, string scopeKey, string functionKey)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "v1/{0}/{1}/{2}/latest.json",
                NormalizeSegment(slotKey, nameof(slotKey)),
                NormalizeSegment(scopeKey, nameof(scopeKey)),
                NormalizeSegment(functionKey, nameof(functionKey)));
        }

        /// <summary>Builds the latest path from an immutable report path.</summary>
        /// <param name="reportPath">Immutable report path.</param>
        /// <returns>Latest pointer path.</returns>
        public static string BuildLatestPathFromReportPath(string reportPath)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                throw new ArgumentException("A report path is required.", nameof(reportPath));
            }

            string[] parts = reportPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 9
                || !string.Equals(parts[0], "v1", StringComparison.Ordinal)
                || !string.Equals(parts[4], "executions", StringComparison.Ordinal)
                || !parts[8].EndsWith(".json", StringComparison.Ordinal))
            {
                throw new ArgumentException("The report path is invalid.", nameof(reportPath));
            }

            return BuildLatestPath(parts[1], parts[2], parts[3]);
        }

        /// <summary>Normalizes and validates a path segment.</summary>
        public static string NormalizeSegment(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The path segment is required.", parameterName);
            }

            string trimmed = value.Trim().ToLowerInvariant();
            if (trimmed.Contains("..", StringComparison.Ordinal)
                || trimmed.Contains('/', StringComparison.Ordinal)
                || trimmed.Contains('\\', StringComparison.Ordinal)
                || !SafeSegment.IsMatch(trimmed))
            {
                throw new ArgumentException("The path segment contains invalid characters.", parameterName);
            }

            return trimmed;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
    }
}
