// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Xunit;

    /// <summary>
    /// Contract, coordinator, path, and storage-boundary tests for function execution reports.
    /// </summary>
    public class FunctionExecutionReportTests
    {
        /// <summary>
        /// All supported outcomes must survive JSON serialization with their canonical enum values.
        /// </summary>
        /// <param name="outcome">Outcome under test.</param>
        [Theory]
        [InlineData(FunctionExecutionReportOutcome.Succeeded)]
        [InlineData(FunctionExecutionReportOutcome.Partial)]
        [InlineData(FunctionExecutionReportOutcome.Failed)]
        [InlineData(FunctionExecutionReportOutcome.Skipped)]
        [InlineData(FunctionExecutionReportOutcome.Disabled)]
        public void Report_RoundTrips_AllSupportedOutcomes(FunctionExecutionReportOutcome outcome)
        {
            DateTime startedAt = new DateTime(2026, 9, 7, 6, 0, 0, DateTimeKind.Utc);
            DateTime completedAt = startedAt.AddSeconds(4);
            var report = new FunctionExecutionReport
            {
                SchemaVersion = 1,
                FunctionKey = "DeleteOldSubscriptionFunction",
                FunctionName = "DeleteOldSubscriptionFunction",
                ExecutionId = "execution-1",
                InvocationId = "invocation-1",
                Environment = "Production",
                SlotKey = "production",
                ScopeKey = "alimentestaideia",
                StartedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                DurationMilliseconds = 4000,
                Outcome = outcome,
                BusinessDataChanged = outcome == FunctionExecutionReportOutcome.Succeeded,
                Activities = new List<FunctionExecutionReportActivity>
                {
                    new FunctionExecutionReportActivity
                    {
                        Ordinal = 1,
                        TimestampUtc = startedAt,
                        ActivityKey = "subscription-candidates",
                        Severity = FunctionExecutionReportActivitySeverity.Information,
                        ScopeKey = "alimentestaideia",
                        Message = "Found 2 candidates.",
                        Counters = new Dictionary<string, long> { ["candidates"] = 2 },
                    },
                },
                Summary = new FunctionExecutionReportSummary
                {
                    ActivitiesAttempted = 1,
                    ActivitiesCompleted = 1,
                    Warnings = 0,
                    Errors = outcome == FunctionExecutionReportOutcome.Failed ? 1 : 0,
                    RecordsChanged = outcome == FunctionExecutionReportOutcome.Succeeded ? 2 : 0,
                },
                Warnings = new List<string>(),
                Errors = outcome == FunctionExecutionReportOutcome.Failed
                    ? new List<string> { "Database operation failed." }
                    : new List<string>(),
            };

            string json = JsonSerializer.Serialize(report);
            FunctionExecutionReport restored = JsonSerializer.Deserialize<FunctionExecutionReport>(json);

            Assert.NotNull(restored);
            Assert.Equal(1, restored.SchemaVersion);
            Assert.Equal(outcome, restored.Outcome);
            Assert.Equal(DateTimeKind.Utc, restored.StartedAtUtc.Kind);
            Assert.Equal("execution-1", restored.ExecutionId);
            Assert.Single(restored.Activities);
            Assert.Equal(
                outcome == FunctionExecutionReportOutcome.Succeeded ? 2 : 0,
                restored.Summary.RecordsChanged);
        }

        /// <summary>
        /// A writer must bound activities and messages and emit one truncation warning.
        /// </summary>
        [Fact]
        public void Writer_BoundsActivitiesAndMessages_WithSingleTruncationWarning()
        {
            var writer = new FunctionExecutionReportWriter(
                new FunctionExecutionReportOptions
                {
                    MaxActivities = 2,
                    MaxActivityMessageLength = 10,
                });

            writer.RecordActivity(
                "first",
                FunctionExecutionReportActivitySeverity.Information,
                "123456789012345");
            writer.RecordActivity(
                "second",
                FunctionExecutionReportActivitySeverity.Information,
                "second");
            writer.RecordActivity(
                "third",
                FunctionExecutionReportActivitySeverity.Information,
                "third");
            writer.RecordActivity(
                "fourth",
                FunctionExecutionReportActivitySeverity.Information,
                "fourth");

            FunctionExecutionReport report = writer.Complete(
                "DeleteOldSubscriptionFunction",
                "execution-1",
                "invocation-1",
                FunctionExecutionReportOutcome.Succeeded,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false);

            Assert.Equal(2, report.Activities.Count);
            Assert.Equal(1, report.Warnings.Count(message => message.Contains("truncat", StringComparison.OrdinalIgnoreCase)));
            Assert.True(report.Activities.All(activity => activity.Message.Length <= 10));
        }

        /// <summary>
        /// Path construction must normalize all scope segments and reject traversal.
        /// </summary>
        [Fact]
        public void Paths_UseNormalizedScopeAndNeverAllowTraversal()
        {
            string path = FunctionExecutionReportPaths.BuildExecutionPath(
                "Production",
                "AlimentaEstaIdeia",
                "DeleteOldSubscriptionFunction",
                new DateTime(2026, 9, 7, 6, 0, 0, DateTimeKind.Utc),
                "execution-1");

            Assert.Equal(
                "v1/production/alimentaestaideia/deleteoldsubscriptionfunction/executions/2026/09/07/execution-1.json",
                path);
            Assert.DoesNotContain("..", path, StringComparison.Ordinal);
            Assert.Throws<ArgumentException>(() => FunctionExecutionReportPaths.BuildExecutionPath(
                "production",
                "../other-tenant",
                "DeleteOldSubscriptionFunction",
                DateTime.UtcNow,
                "execution-1"));
        }

        /// <summary>
        /// A newer completion must remain latest when an older execution finishes afterwards.
        /// </summary>
        [Fact]
        public async Task Coordinator_DoesNotLetOlderCompletionReplaceLatest()
        {
            var store = new RecordingFunctionExecutionReportStore();
            var coordinator = new FunctionExecutionReportCoordinator(
                store,
                new FunctionExecutionReportOptions(),
                new FixedUtcClock(new DateTime(2026, 9, 7, 6, 0, 0, DateTimeKind.Utc)));

            await coordinator.FinalizeAsync(CreateReport("newer", new DateTime(2026, 9, 7, 6, 2, 0, DateTimeKind.Utc)));
            await coordinator.FinalizeAsync(CreateReport("older", new DateTime(2026, 9, 7, 6, 1, 0, DateTimeKind.Utc)));

            Assert.Equal("newer", store.LatestPointer.ExecutionId);
            Assert.Equal(2, store.ImmutableReports.Count);
        }

        /// <summary>
        /// Storage failures must be observable without changing the business outcome.
        /// </summary>
        [Fact]
        public async Task Coordinator_PreservesBusinessOutcome_WhenStorageFails()
        {
            var store = new RecordingFunctionExecutionReportStore
            {
                ThrowOnWrite = new InvalidOperationException("storage unavailable"),
            };
            var coordinator = new FunctionExecutionReportCoordinator(
                store,
                new FunctionExecutionReportOptions(),
                new FixedUtcClock(DateTime.UtcNow));
            FunctionExecutionReport report = CreateReport("failed-storage", DateTime.UtcNow);

            FunctionExecutionReportStorageResult result = await coordinator.FinalizeAsync(report);

            Assert.Equal(FunctionExecutionReportOutcome.Succeeded, result.BusinessOutcome);
            Assert.False(result.ReportPersisted);
            Assert.Contains("storage", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Read failures must remain distinguishable from an absent report or corrupt JSON.
        /// </summary>
        [Theory]
        [InlineData(FunctionExecutionReportStorageState.Missing)]
        [InlineData(FunctionExecutionReportStorageState.Corrupt)]
        [InlineData(FunctionExecutionReportStorageState.Expired)]
        [InlineData(FunctionExecutionReportStorageState.Unavailable)]
        public async Task Reader_ExposesTypedUnavailableStates(FunctionExecutionReportStorageState state)
        {
            var reader = new FakeFunctionExecutionReportReader(state);

            FunctionExecutionReportStorageResult result = await reader.GetExecutionAsync(
                new FunctionExecutionReportScope("production", "alimentestaideia"),
                "DeleteOldSubscriptionFunction",
                "execution-1");

            Assert.Equal(state, result.State);
            Assert.Null(result.Report);
        }

        /// <summary>
        /// Default retention must be at least 90 days and shorter values must be rejected.
        /// </summary>
        [Fact]
        public void Options_EnforceMinimumRetention()
        {
            Assert.True(new FunctionExecutionReportOptions().RetentionDays >= 90);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                FunctionExecutionReportOptions.Validate(new FunctionExecutionReportOptions { RetentionDays = 89 }));
        }

        /// <summary>
        /// Report messages must reject secret-shaped payloads before persistence.
        /// </summary>
        [Theory]
        [InlineData("Authorization: Bearer abc")]
        [InlineData("DefaultEndpointsProtocol=https;AccountKey=secret")]
        [InlineData("ApiCertificateV3=secret")]
        [InlineData("password=secret")]
        [InlineData("clientSecret=secret")]
        [InlineData("connectionString=secret")]
        [InlineData("{\"connectionString\":\"secret\"}")]
        [InlineData("access_token=secret")]
        [InlineData("api_key: secret")]
        [InlineData("api_key=secret")]
        [InlineData("secret: secret")]
        [InlineData("Authorization : Bearer abc.def-123")]
        [InlineData("Bearer abc.def-123")]
        [InlineData("api  key : secret")]
        [InlineData("api-key=secret")]
        [InlineData("access\ttoken : secret")]
        [InlineData("client.secret: secret")]
        [InlineData("client secret")]
        [InlineData("connection-string : secret")]
        [InlineData("connection string")]
        [InlineData("secret : secret")]
        public void Writer_RejectsSensitiveMessages(string message)
        {
            var writer = new FunctionExecutionReportWriter(new FunctionExecutionReportOptions());

            Assert.Throws<ArgumentException>(() => writer.RecordActivity(
                "unsafe",
                FunctionExecutionReportActivitySeverity.Error,
                message));
        }

        /// <summary>Errors and explicit outcome marks must override an optimistic completion outcome.</summary>
        [Fact]
        public async Task Coordinator_UsesMarkedFailure_WhenDelegateCompletesOptimistically()
        {
            var store = new RecordingFunctionExecutionReportStore();
            var coordinator = new FunctionExecutionReportCoordinator(store, new FunctionExecutionReportOptions());
            IFunctionExecutionReportExecution execution = coordinator.Begin(
                "UpdateSubscriptions",
                new FunctionExecutionReportScope("production", "alimentestaideia"),
                "invocation-1",
                "UpdateSubscriptions",
                "TimerTrigger",
                "Production",
                "operation-1");

            execution.RecordError("The transaction failed.");
            FunctionExecutionReportStorageResult result = await execution.CompleteAsync(
                FunctionExecutionReportOutcome.Succeeded,
                false,
                "Completed.");

            Assert.Equal(FunctionExecutionReportOutcome.Failed, result.BusinessOutcome);
            Assert.Equal(FunctionExecutionReportOutcome.Failed, result.Report.Outcome);
            Assert.Equal("TimerTrigger", result.Report.TriggerType);
            Assert.Equal("Production", result.Report.Environment);
            Assert.Equal("operation-1", result.Report.CorrelationId);
        }

        /// <summary>Summary counters expose bounded tenant totals.</summary>
        [Fact]
        public void Writer_ProjectsTenantCountersIntoSummary()
        {
            var writer = new FunctionExecutionReportWriter(new FunctionExecutionReportOptions());
            writer.SetCounter("tenantsDiscovered", 4);
            writer.SetCounter("tenantsProcessed", 4);
            writer.SetCounter("tenantsSucceeded", 3);
            writer.SetCounter("tenantsFailed", 1);
            writer.SetCounter("tenantsSkipped", 0);

            FunctionExecutionReport report = writer.Complete(
                "UpdateSubscriptions",
                "execution-1",
                "invocation-1",
                FunctionExecutionReportOutcome.Partial,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false);

            Assert.Equal(4, report.Summary.TenantsDiscovered);
            Assert.Equal(4, report.Summary.TenantsProcessed);
            Assert.Equal(3, report.Summary.TenantsSucceeded);
            Assert.Equal(1, report.Summary.TenantsFailed);
            Assert.Equal(0, report.Summary.TenantsSkipped);
        }

        /// <summary>Warnings, errors, and counters remain bounded.</summary>
        [Fact]
        public void Writer_BoundsWarningsErrorsAndCounters()
        {
            var writer = new FunctionExecutionReportWriter(
                new FunctionExecutionReportOptions
                {
                    MaxWarnings = 1,
                    MaxErrors = 1,
                    MaxCounters = 1,
                });

            writer.RecordWarning("warning-1");
            writer.RecordWarning("warning-2");
            writer.RecordError("error-1");
            writer.RecordError("error-2");
            writer.SetCounter("counter-1", 1);
            writer.SetCounter("counter-2", 2);

            FunctionExecutionReport report = writer.Complete(
                "UpdateSubscriptions",
                "execution-1",
                "invocation-1",
                FunctionExecutionReportOutcome.Partial,
                DateTime.UtcNow,
                DateTime.UtcNow,
                false);

            Assert.Single(report.Warnings);
            Assert.Single(report.Errors);
            Assert.Single(report.Summary.Counters);
        }

        /// <summary>Azure lifecycle rule descriptors retain data outside function execution.</summary>
        [Fact]
        public void LifecyclePolicy_UsesConfiguredRetentionAndPrefix()
        {
            FunctionExecutionReportLifecyclePolicy policy = FunctionExecutionReportLifecyclePolicy.Create(
                new FunctionExecutionReportOptions { RetentionDays = 120, LifecyclePrefix = "v1/" });

            Assert.Equal("function-execution-reports-retention", policy.Name);
            Assert.Equal("v1/", Assert.Single(policy.PrefixMatch));
            Assert.Equal(120, policy.DeleteAfterDaysSinceModificationGreaterThan);
        }

        /// <summary>Equal timestamps use execution ID as a deterministic tie-breaker.</summary>
        [Fact]
        public void LatestPointer_UsesExecutionIdAsTieBreaker()
        {
            DateTime completedAt = new DateTime(2026, 9, 7, 6, 0, 0, DateTimeKind.Utc);
            var existing = new FunctionExecutionReportLatestPointer { CompletedAtUtc = completedAt, ExecutionId = "z" };
            var candidate = new FunctionExecutionReportLatestPointer { CompletedAtUtc = completedAt, ExecutionId = "a" };

            Assert.True(FunctionExecutionReportLatestPointer.IsAtLeastAsNew(existing, candidate));
            Assert.False(FunctionExecutionReportLatestPointer.IsAtLeastAsNew(candidate, existing));
        }

        private static FunctionExecutionReport CreateReport(string executionId, DateTime completedAtUtc)
        {
            return new FunctionExecutionReport
            {
                SchemaVersion = 1,
                FunctionKey = "DeleteOldSubscriptionFunction",
                FunctionName = "DeleteOldSubscriptionFunction",
                ExecutionId = executionId,
                InvocationId = "invocation-1",
                SlotKey = "production",
                ScopeKey = "alimentestaideia",
                StartedAtUtc = completedAtUtc.AddMinutes(-1),
                CompletedAtUtc = completedAtUtc,
                Outcome = FunctionExecutionReportOutcome.Succeeded,
                Summary = new FunctionExecutionReportSummary(),
            };
        }

        private sealed class FixedUtcClock : IFunctionExecutionReportClock
        {
            private readonly DateTime utcNow;

            public FixedUtcClock(DateTime utcNow)
            {
                this.utcNow = utcNow;
            }

            public DateTime UtcNow => this.utcNow;
        }

        private sealed class RecordingFunctionExecutionReportStore : IFunctionExecutionReportStore
        {
            public List<FunctionExecutionReport> ImmutableReports { get; } = new List<FunctionExecutionReport>();

            public FunctionExecutionReportLatestPointer LatestPointer { get; private set; }

            public Exception ThrowOnWrite { get; init; }

            public Task WriteExecutionAsync(FunctionExecutionReport report, CancellationToken cancellationToken = default)
            {
                if (this.ThrowOnWrite != null)
                {
                    throw this.ThrowOnWrite;
                }

                this.ImmutableReports.Add(report);
                return Task.CompletedTask;
            }

            public Task PublishLatestAsync(FunctionExecutionReportLatestPointer pointer, CancellationToken cancellationToken = default)
            {
                if (this.LatestPointer == null || pointer.CompletedAtUtc >= this.LatestPointer.CompletedAtUtc)
                {
                    this.LatestPointer = pointer;
                }

                return Task.CompletedTask;
            }
        }

        private sealed class FakeFunctionExecutionReportReader : IFunctionExecutionReportReader
        {
            private readonly FunctionExecutionReportStorageState state;

            public FakeFunctionExecutionReportReader(FunctionExecutionReportStorageState state)
            {
                this.state = state;
            }

            public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(
                FunctionExecutionReportScope scope,
                string functionKey,
                string executionId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new FunctionExecutionReportStorageResult
                {
                    State = this.state,
                });
            }
        }
    }
}
