// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportCoordinator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Creates and finalizes bounded reports without changing business outcomes.</summary>
    public sealed class FunctionExecutionReportCoordinator : IFunctionExecutionReportCoordinator
    {
        private readonly IFunctionExecutionReportStore store;
        private readonly FunctionExecutionReportOptions options;
        private readonly IFunctionExecutionReportClock clock;
        private readonly IFunctionExecutionReportIdGenerator idGenerator;

        /// <summary>Initializes a coordinator.</summary>
        public FunctionExecutionReportCoordinator(
            IFunctionExecutionReportStore store,
            FunctionExecutionReportOptions options,
            IFunctionExecutionReportClock clock = null,
            IFunctionExecutionReportIdGenerator idGenerator = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            FunctionExecutionReportOptions.Validate(this.options);
            this.clock = clock ?? new SystemUtcClock();
            this.idGenerator = idGenerator ?? new GuidFunctionExecutionReportIdGenerator();
        }

        /// <inheritdoc />
        public IFunctionExecutionReportExecution Begin(
            string functionKey,
            FunctionExecutionReportScope scope,
            string invocationId = null,
            string functionName = null,
            string triggerType = null,
            string environment = null,
            string correlationId = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            DateTime startedAtUtc = this.clock.UtcNow.ToUniversalTime();
            return new Execution(
                this,
                new FunctionExecutionReportWriter(this.options),
                functionKey,
                scope,
                this.idGenerator.CreateId(),
                invocationId ?? this.idGenerator.CreateId(),
                functionName ?? functionKey,
                triggerType ?? "Unknown",
                environment ?? GetEnvironmentName(),
                correlationId ?? invocationId,
                startedAtUtc);
        }

        /// <inheritdoc />
        public async Task<FunctionExecutionReportStorageResult> FinalizeAsync(FunctionExecutionReport report, CancellationToken cancellationToken = default)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var result = new FunctionExecutionReportStorageResult
            {
                State = FunctionExecutionReportStorageState.Failed,
                BusinessOutcome = report.Outcome,
                Report = report,
            };

            if (!this.options.Enabled)
            {
                result.State = FunctionExecutionReportStorageState.Unavailable;
                result.Diagnostic = "Execution report persistence is disabled.";
                return result;
            }

            try
            {
                await this.store.WriteExecutionAsync(report, cancellationToken).ConfigureAwait(false);
                var pointer = new FunctionExecutionReportLatestPointer
                {
                    ExecutionId = report.ExecutionId,
                    CompletedAtUtc = report.CompletedAtUtc,
                    Outcome = report.Outcome,
                    DurationMilliseconds = report.DurationMilliseconds,
                    ReportPath = FunctionExecutionReportPaths.BuildExecutionPath(report.SlotKey, report.ScopeKey, report.FunctionKey, report.CompletedAtUtc, report.ExecutionId),
                    Summary = report.Summary?.Message ?? string.Empty,
                };
                await this.store.PublishLatestAsync(pointer, cancellationToken).ConfigureAwait(false);
                result.State = FunctionExecutionReportStorageState.Succeeded;
                result.ReportPersisted = true;
                result.LatestPointer = pointer;
                result.Diagnostic = "Execution report persisted.";
                return result;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                result.State = FunctionExecutionReportStorageState.Unavailable;
                result.Diagnostic = "Execution report persistence failed: " + exception.Message;
                result.Exception = exception;
                return result;
            }
        }

        private static string GetEnvironmentName()
        {
            return System.Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Unknown";
        }

        private sealed class Execution : IFunctionExecutionReportExecution
        {
            private readonly FunctionExecutionReportCoordinator coordinator;
            private readonly FunctionExecutionReportWriter writer;
            private readonly string functionKey;
            private readonly FunctionExecutionReportScope scope;
            private readonly string invocationId;
            private readonly string functionName;
            private readonly string triggerType;
            private readonly string environment;
            private readonly string correlationId;
            private readonly DateTime startedAtUtc;
            private bool completed;
            private FunctionExecutionReportOutcome? markedOutcome;

            public Execution(
                FunctionExecutionReportCoordinator coordinator,
                FunctionExecutionReportWriter writer,
                string functionKey,
                FunctionExecutionReportScope scope,
                string executionId,
                string invocationId,
                string functionName,
                string triggerType,
                string environment,
                string correlationId,
                DateTime startedAtUtc)
            {
                this.coordinator = coordinator;
                this.writer = writer;
                this.functionKey = functionKey;
                this.scope = scope;
                this.ExecutionId = executionId;
                this.invocationId = invocationId;
                this.functionName = functionName;
                this.triggerType = triggerType;
                this.environment = environment;
                this.correlationId = correlationId;
                this.startedAtUtc = startedAtUtc;
            }

            public string ExecutionId { get; }

            public void RecordActivity(string activityKey, FunctionExecutionReportActivitySeverity severity, string message, IReadOnlyDictionary<string, long> counters = null)
            {
                this.ThrowIfCompleted();
                this.writer.RecordActivity(activityKey, severity, message, this.scope.ScopeKey, counters, this.coordinator.clock.UtcNow);
            }

            public void RecordWarning(string message)
            {
                this.ThrowIfCompleted();
                this.writer.RecordWarning(message);
            }

            public void RecordError(string message)
            {
                this.ThrowIfCompleted();
                this.markedOutcome = FunctionExecutionReportOutcome.Failed;
                this.writer.RecordError(message);
            }

            public void MarkOutcome(FunctionExecutionReportOutcome outcome)
            {
                this.ThrowIfCompleted();
                this.markedOutcome = outcome;
            }

            public void SetCounter(string key, long value)
            {
                this.ThrowIfCompleted();
                this.writer.SetCounter(key, value);
            }

            public Task<FunctionExecutionReportStorageResult> CompleteAsync(FunctionExecutionReportOutcome outcome, bool businessDataChanged, string summaryMessage = null, CancellationToken cancellationToken = default)
            {
                this.ThrowIfCompleted();
                this.completed = true;
                DateTime completedAtUtc = this.coordinator.clock.UtcNow.ToUniversalTime();
                FunctionExecutionReportOutcome effectiveOutcome = outcome == FunctionExecutionReportOutcome.Succeeded && this.markedOutcome.HasValue
                    ? this.markedOutcome.Value
                    : outcome;
                FunctionExecutionReport report = this.writer.Complete(
                    this.functionKey,
                    this.ExecutionId,
                    this.invocationId,
                    effectiveOutcome,
                    this.startedAtUtc,
                    completedAtUtc,
                    businessDataChanged,
                    this.functionName,
                    this.environment,
                    this.scope.SlotKey,
                    this.scope.ScopeKey,
                    summaryMessage,
                    this.triggerType,
                    this.correlationId);
                return this.coordinator.FinalizeAsync(report, cancellationToken);
            }

            private void ThrowIfCompleted()
            {
                if (this.completed)
                {
                    throw new InvalidOperationException("The execution report has already been completed.");
                }
            }
        }
    }
}
