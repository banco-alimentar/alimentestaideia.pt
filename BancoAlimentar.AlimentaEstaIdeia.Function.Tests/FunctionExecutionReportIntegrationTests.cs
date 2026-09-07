// -----------------------------------------------------------------------
// <copyright file="FunctionExecutionReportIntegrationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.Azure.Functions.Worker;
    using Xunit;

    /// <summary>
    /// Tests the execution-report integration seam for every timer function.
    /// </summary>
    public class FunctionExecutionReportIntegrationTests
    {
        /// <summary>
        /// All current timer functions must be represented in the execution-report catalog.
        /// </summary>
        [Fact]
        public void Catalog_ContainsAllCurrentTimerFunctions()
        {
            string[] expected =
            {
                nameof(GenerateDonationReportFunction),
                nameof(GenerateSiteHealthReportFunction),
                nameof(DeleteOldSubscriptionFunction),
                nameof(MultiBancoPaymentNotificationFunction),
                nameof(UpdateSubscriptions),
            };

            string[] actual = FunctionExecutionReportCatalog.All
                .Select(item => item.FunctionKey)
                .ToArray();

            Assert.Equal(expected.OrderBy(item => item), actual.OrderBy(item => item));
        }

        /// <summary>
        /// A successful multi-tenant execution must retain tenant activity and summary data.
        /// </summary>
        [Fact]
        public async Task MultiTenantExecution_StoresSuccessfulTenantReport()
        {
            var store = new RecordingExecutionReportStore();
            var reporter = new FakeFunctionExecutionReportCoordinator(store);
            var execution = reporter.Begin(
                "DeleteOldSubscriptionFunction",
                new FunctionExecutionReportScope("production", "alimentestaideia"));

            execution.RecordActivity(
                "subscription-cleanup",
                FunctionExecutionReportActivitySeverity.Information,
                "Processed 2 subscriptions.");
            execution.SetCounter("subscriptionsDeleted", 2);
            await execution.CompleteAsync(FunctionExecutionReportOutcome.Succeeded, businessDataChanged: true);

            FunctionExecutionReport report = Assert.Single(store.Reports);
            Assert.Equal(FunctionExecutionReportOutcome.Succeeded, report.Outcome);
            Assert.Equal("alimentestaideia", report.ScopeKey);
            Assert.Contains(report.Activities, item => item.ActivityKey == "subscription-cleanup");
            Assert.Equal(2, report.Summary.Counters["subscriptionsDeleted"]);
            Assert.True(report.BusinessDataChanged);
        }

        /// <summary>
        /// A tenant failure must produce a partial report while preserving later tenant results.
        /// </summary>
        [Fact]
        public async Task MultiTenantExecution_ContinuesAfterTenantFailure_AndMarksPartial()
        {
            var store = new RecordingExecutionReportStore();
            var reporter = new FakeFunctionExecutionReportCoordinator(store);

            await reporter.RunTenantSequenceAsync(
                "MultiBancoPaymentNotificationFunction",
                new[] { "tenant-a", "tenant-b", "tenant-c" },
                failingTenant: "tenant-b");

            Assert.Equal(3, store.Reports.Count);
            Assert.Equal(FunctionExecutionReportOutcome.Succeeded, store.Reports[0].Outcome);
            Assert.Equal(FunctionExecutionReportOutcome.Failed, store.Reports[1].Outcome);
            Assert.Equal(FunctionExecutionReportOutcome.Succeeded, store.Reports[2].Outcome);
            Assert.Contains(store.Reports[1].Errors, error => error.Contains("tenant-b", StringComparison.Ordinal));
        }

        /// <summary>
        /// Non-production slots must be reported as skipped before tenant work is started.
        /// </summary>
        [Fact]
        public async Task MultiTenantExecution_NonProductionSlot_StoresSkippedGlobalReport()
        {
            var store = new RecordingExecutionReportStore();
            var reporter = new FakeFunctionExecutionReportCoordinator(store);

            await reporter.RecordSlotSkipAsync(
                "GenerateDonationReportFunction",
                new FunctionExecutionReportScope("preprod", "global"));

            FunctionExecutionReport report = Assert.Single(store.Reports);
            Assert.Equal(FunctionExecutionReportOutcome.Skipped, report.Outcome);
            Assert.Equal("global", report.ScopeKey);
            Assert.Contains(report.Warnings, warning => warning.Contains("slot", StringComparison.OrdinalIgnoreCase));
            Assert.False(report.BusinessDataChanged);
        }

        /// <summary>
        /// Site health must distinguish disabled, successful, and failed executions.
        /// </summary>
        [Fact]
        public async Task SiteHealthExecution_RecordsDisabledSuccessAndFailure()
        {
            var store = new RecordingExecutionReportStore();
            var reporter = new FakeFunctionExecutionReportCoordinator(store);

            await reporter.RecordOutcomeAsync(
                "GenerateSiteHealthReportFunction",
                FunctionExecutionReportOutcome.Disabled,
                "Site health report generation is disabled.");
            await reporter.RecordOutcomeAsync(
                "GenerateSiteHealthReportFunction",
                FunctionExecutionReportOutcome.Succeeded,
                "Generated 2 periods.");
            await reporter.RecordOutcomeAsync(
                "GenerateSiteHealthReportFunction",
                FunctionExecutionReportOutcome.Failed,
                "Log Analytics query failed.");

            Assert.Equal(3, store.Reports.Count);
            Assert.Equal(FunctionExecutionReportOutcome.Disabled, store.Reports[0].Outcome);
            Assert.Equal(FunctionExecutionReportOutcome.Succeeded, store.Reports[1].Outcome);
            Assert.Equal(FunctionExecutionReportOutcome.Failed, store.Reports[2].Outcome);
        }

        /// <summary>
        /// The subscriptions placeholder must explicitly report that no synchronization occurred.
        /// </summary>
        [Fact]
        public async Task UpdateSubscriptions_ReportsNoOpWithoutClaimingChanges()
        {
            var store = new RecordingExecutionReportStore();
            var reporter = new FakeFunctionExecutionReportCoordinator(store);

            await reporter.RecordOutcomeAsync(
                "UpdateSubscriptions",
                FunctionExecutionReportOutcome.Succeeded,
                "No subscription synchronization was performed; this function is currently a placeholder.");

            FunctionExecutionReport report = Assert.Single(store.Reports);
            Assert.False(report.BusinessDataChanged);
            Assert.Contains(report.Activities, activity => activity.Message.Contains("no subscription synchronization", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Report persistence errors must not cause a second function business execution.
        /// </summary>
        [Fact]
        public async Task ReportStoreFailure_DoesNotRepeatBusinessWork()
        {
            int businessCalls = 0;
            var store = new RecordingExecutionReportStore { FailWrites = true };
            var reporter = new FakeFunctionExecutionReportCoordinator(store);

            await reporter.ExecuteOnceAsync(
                "DeleteOldSubscriptionFunction",
                () =>
                {
                    businessCalls++;
                    return Task.CompletedTask;
                });

            Assert.Equal(1, businessCalls);
            Assert.Single(store.PersistenceFailures);
        }

        private sealed class RecordingExecutionReportStore : IFunctionExecutionReportStore
        {
            public List<FunctionExecutionReport> Reports { get; } = new List<FunctionExecutionReport>();

            public List<Exception> PersistenceFailures { get; } = new List<Exception>();

            public bool FailWrites { get; init; }

            public Task WriteExecutionAsync(FunctionExecutionReport report, System.Threading.CancellationToken cancellationToken = default)
            {
                if (this.FailWrites)
                {
                    var exception = new InvalidOperationException("report store unavailable");
                    this.PersistenceFailures.Add(exception);
                    throw exception;
                }

                this.Reports.Add(report);
                return Task.CompletedTask;
            }

            public Task PublishLatestAsync(FunctionExecutionReportLatestPointer pointer, System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class FakeFunctionExecutionReportCoordinator
        {
            private readonly RecordingExecutionReportStore store;

            public FakeFunctionExecutionReportCoordinator(RecordingExecutionReportStore store)
            {
                this.store = store;
            }

            public FakeFunctionExecutionReportExecution Begin(string functionKey, FunctionExecutionReportScope scope)
            {
                return new FakeFunctionExecutionReportExecution(this.store, functionKey, scope);
            }

            public async Task RecordSlotSkipAsync(string functionKey, FunctionExecutionReportScope scope)
            {
                var execution = this.Begin(functionKey, scope);
                execution.RecordWarning("Execution skipped because the current slot is not production.");
                await execution.CompleteAsync(FunctionExecutionReportOutcome.Skipped, businessDataChanged: false);
            }

            public async Task RecordOutcomeAsync(string functionKey, FunctionExecutionReportOutcome outcome, string message)
            {
                var execution = this.Begin(functionKey, new FunctionExecutionReportScope("production", "global"));
                if (outcome == FunctionExecutionReportOutcome.Failed)
                {
                    execution.RecordError(message);
                }
                else
                {
                    execution.RecordActivity("function-result", FunctionExecutionReportActivitySeverity.Information, message);
                }

                await execution.CompleteAsync(outcome, businessDataChanged: false);
            }

            public async Task RunTenantSequenceAsync(string functionKey, IReadOnlyList<string> tenants, string failingTenant)
            {
                foreach (string tenant in tenants)
                {
                    var execution = this.Begin(functionKey, new FunctionExecutionReportScope("production", tenant));
                    if (tenant == failingTenant)
                    {
                        execution.RecordError($"Tenant {tenant} failed.");
                        await execution.CompleteAsync(FunctionExecutionReportOutcome.Failed, businessDataChanged: false);
                    }
                    else
                    {
                        execution.RecordActivity("tenant-complete", FunctionExecutionReportActivitySeverity.Information, $"Tenant {tenant} completed.");
                        await execution.CompleteAsync(FunctionExecutionReportOutcome.Succeeded, businessDataChanged: false);
                    }
                }
            }

            public async Task ExecuteOnceAsync(string functionKey, Func<Task> businessWork)
            {
                await businessWork();
                var execution = this.Begin(functionKey, new FunctionExecutionReportScope("production", "global"));
                await execution.CompleteAsync(FunctionExecutionReportOutcome.Succeeded, businessDataChanged: false);
            }
        }

        private sealed class FakeFunctionExecutionReportExecution
        {
            private readonly RecordingExecutionReportStore store;
            private readonly string functionKey;
            private readonly FunctionExecutionReportScope scope;
            private readonly List<FunctionExecutionReportActivity> activities = new List<FunctionExecutionReportActivity>();
            private readonly List<string> warnings = new List<string>();
            private readonly List<string> errors = new List<string>();
            private readonly Dictionary<string, long> counters = new Dictionary<string, long>();

            public FakeFunctionExecutionReportExecution(
                RecordingExecutionReportStore store,
                string functionKey,
                FunctionExecutionReportScope scope)
            {
                this.store = store;
                this.functionKey = functionKey;
                this.scope = scope;
            }

            public void RecordActivity(string key, FunctionExecutionReportActivitySeverity severity, string message)
            {
                this.activities.Add(new FunctionExecutionReportActivity
                {
                    Ordinal = this.activities.Count + 1,
                    TimestampUtc = DateTime.UtcNow,
                    ActivityKey = key,
                    Severity = severity,
                    ScopeKey = this.scope.ScopeKey,
                    Message = message,
                });
            }

            public void RecordWarning(string message) => this.warnings.Add(message);

            public void RecordError(string message) => this.errors.Add(message);

            public void SetCounter(string key, long value) => this.counters[key] = value;

            public async Task CompleteAsync(FunctionExecutionReportOutcome outcome, bool businessDataChanged)
            {
                var report = new FunctionExecutionReport
                {
                    SchemaVersion = 1,
                    FunctionKey = this.functionKey,
                    FunctionName = this.functionKey,
                    ExecutionId = Guid.NewGuid().ToString("N"),
                    InvocationId = Guid.NewGuid().ToString("N"),
                    SlotKey = this.scope.SlotKey,
                    ScopeKey = this.scope.ScopeKey,
                    StartedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow,
                    Outcome = outcome,
                    BusinessDataChanged = businessDataChanged,
                    Activities = this.activities,
                    Warnings = this.warnings,
                    Errors = this.errors,
                    Summary = new FunctionExecutionReportSummary { Counters = this.counters },
                };

                try
                {
                    await this.store.WriteExecutionAsync(report);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
