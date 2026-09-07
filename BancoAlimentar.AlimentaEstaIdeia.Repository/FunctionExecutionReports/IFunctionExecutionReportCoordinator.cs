// -----------------------------------------------------------------------
// <copyright file="IFunctionExecutionReportCoordinator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Coordinates report execution lifecycle and persistence.</summary>
    public interface IFunctionExecutionReportCoordinator
    {
        /// <summary>Starts a report execution.</summary>
        IFunctionExecutionReportExecution Begin(
            string functionKey,
            FunctionExecutionReportScope scope,
            string invocationId = null,
            string functionName = null,
            string triggerType = null,
            string environment = null,
            string correlationId = null);

        /// <summary>Persists a completed report.</summary>
        Task<FunctionExecutionReportStorageResult> FinalizeAsync(FunctionExecutionReport report, CancellationToken cancellationToken = default);
    }

    /// <summary>Mutable lifecycle context for one report.</summary>
    public interface IFunctionExecutionReportExecution
    {
        /// <summary>Gets the execution identifier.</summary>
        string ExecutionId { get; }

        /// <summary>Records activity.</summary>
        void RecordActivity(string activityKey, FunctionExecutionReportActivitySeverity severity, string message, IReadOnlyDictionary<string, long> counters = null);

        /// <summary>Records a warning.</summary>
        void RecordWarning(string message);

        /// <summary>Records an error.</summary>
        void RecordError(string message);

        /// <summary>Marks the execution outcome before finalization.</summary>
        void MarkOutcome(FunctionExecutionReportOutcome outcome);

        /// <summary>Sets a summary counter.</summary>
        void SetCounter(string key, long value);

        /// <summary>Completes and persists the report.</summary>
        Task<FunctionExecutionReportStorageResult> CompleteAsync(FunctionExecutionReportOutcome outcome, bool businessDataChanged, string summaryMessage = null, CancellationToken cancellationToken = default);
    }
}
