// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionCommand.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;

    /// <summary>
    /// A validated request to execute one catalogued Azure Function.
    /// </summary>
    public sealed class FunctionExecutionCommand
    {
        /// <summary>Gets the command schema version.</summary>
        public int SchemaVersion { get; init; } = 1;

        /// <summary>Gets the stable catalog function key.</summary>
        public string FunctionKey { get; init; }

        /// <summary>Gets the unique command identifier.</summary>
        public string CommandId { get; init; }

        /// <summary>Gets the UTC time at which the command was requested.</summary>
        public DateTimeOffset RequestedAtUtc { get; init; }

        /// <summary>Gets the source trigger name.</summary>
        public string TriggerType { get; init; }

        /// <summary>Gets the wire-compatible trigger source name.</summary>
        public string TriggerSource { get; init; }

        /// <summary>Gets the safe correlation identifier used by the execution report.</summary>
        public string CorrelationId { get; init; }

        /// <summary>Validates the command without resolving arbitrary types or methods.</summary>
        /// <returns>A validation error, or null when the command is valid.</returns>
        public string Validate()
        {
            if (this.SchemaVersion != 1)
            {
                return "Unsupported command schema version.";
            }

            if (FunctionExecutionReportCatalog.Find(this.FunctionKey) == null)
            {
                return "The function key is not present in the execution catalog.";
            }

            if (!Guid.TryParse(this.CommandId, out _))
            {
                return "The command identifier is invalid.";
            }

            if (this.RequestedAtUtc == default)
            {
                return "The requested timestamp is required.";
            }

            if (!string.Equals(this.GetTriggerType(), "AdminManualTrigger", StringComparison.Ordinal))
            {
                return "The command trigger type is invalid.";
            }

            if (!string.IsNullOrWhiteSpace(this.CorrelationId) && this.CorrelationId.Length > 128)
            {
                return "The correlation identifier is too long.";
            }

            return null;
        }

        /// <summary>Gets the normalized trigger type from either supported wire property.</summary>
        /// <returns>The manual trigger type.</returns>
        public string GetTriggerType()
        {
            return string.IsNullOrWhiteSpace(this.TriggerType) ? this.TriggerSource : this.TriggerType;
        }
    }
}
