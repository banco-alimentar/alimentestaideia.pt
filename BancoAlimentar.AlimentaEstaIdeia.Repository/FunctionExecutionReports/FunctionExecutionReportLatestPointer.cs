// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportLatestPointer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>Small document pointing to the latest completed execution.</summary>
    public sealed class FunctionExecutionReportLatestPointer
    {
        /// <summary>Gets or sets the execution identifier.</summary>
        public string ExecutionId { get; set; } = string.Empty;

        /// <summary>Gets or sets the UTC completion timestamp.</summary>
        public DateTime CompletedAtUtc { get; set; }

        /// <summary>Gets or sets the outcome.</summary>
        public FunctionExecutionReportOutcome Outcome { get; set; }

        /// <summary>Gets or sets the duration.</summary>
        public long DurationMilliseconds { get; set; }

        /// <summary>Gets or sets the immutable report path.</summary>
        public string ReportPath { get; set; } = string.Empty;

        /// <summary>Gets or sets the safe human-readable execution summary.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Determines whether the existing pointer wins the deterministic latest ordering.</summary>
        public static bool IsAtLeastAsNew(FunctionExecutionReportLatestPointer existing, FunctionExecutionReportLatestPointer candidate)
        {
            ArgumentNullException.ThrowIfNull(existing);
            ArgumentNullException.ThrowIfNull(candidate);
            int timestampComparison = existing.CompletedAtUtc.CompareTo(candidate.CompletedAtUtc);
            return timestampComparison > 0
                || (timestampComparison == 0
                    && string.Compare(existing.ExecutionId, candidate.ExecutionId, StringComparison.Ordinal) >= 0);
        }
    }
}
