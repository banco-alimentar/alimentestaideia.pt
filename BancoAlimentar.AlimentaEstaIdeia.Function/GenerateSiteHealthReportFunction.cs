// -----------------------------------------------------------------------
// <copyright file="GenerateSiteHealthReportFunction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Generates the daily Application Insights site health report for webmasters.
    /// </summary>
    public class GenerateSiteHealthReportFunction
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IConfiguration configuration;
        private readonly FunctionExecutionReportCoordinatorFactory reportCoordinatorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateSiteHealthReportFunction"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Application Insights configuration.</param>
        /// <param name="configuration">Function configuration.</param>
        /// <param name="reportCoordinatorFactory">Execution report coordinator factory.</param>
        public GenerateSiteHealthReportFunction(
            TelemetryConfiguration telemetryConfiguration,
            IConfiguration configuration,
            FunctionExecutionReportCoordinatorFactory reportCoordinatorFactory = null)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.configuration = configuration;
            this.reportCoordinatorFactory = reportCoordinatorFactory ?? new FunctionExecutionReportCoordinatorFactory();
        }

        /// <summary>
        /// Timer entry point — runs daily at 07:00 UTC (after donation report).
        /// </summary>
        /// <param name="timer">Timer metadata.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A task.</returns>
        [Function("GenerateSiteHealthReportFunction")]
        public Task Run([TimerTrigger("0 0 7 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            return this.RunCoreAsync("TimerTrigger", Guid.NewGuid().ToString("N"), null, log);
        }

        /// <summary>Runs site-health generation for a validated manual command.</summary>
        /// <param name="command">Validated command.</param>
        /// <returns>A task object to monitor progress.</returns>
        public Task RunManualAsync(FunctionExecutionCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return this.RunCoreAsync(command.GetTriggerType(), command.CommandId, command.CorrelationId, null);
        }

        private string GetReportEnvironment()
        {
            return Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Unknown";
        }

        private async Task RunCoreAsync(string triggerType, string invocationId, string correlationId, ILogger log)
        {
            correlationId ??= this.GetCorrelationId(invocationId);
            IFunctionExecutionReportExecution executionReport = this.reportCoordinatorFactory.Create(this.configuration).Begin(
                nameof(GenerateSiteHealthReportFunction),
                new FunctionExecutionReportScope(FunctionSlotExecution.GetSlotKey(), "global"),
                invocationId,
                nameof(GenerateSiteHealthReportFunction),
                triggerType: triggerType,
                environment: this.GetReportEnvironment(),
                correlationId: correlationId);
            if (!FunctionSlotExecution.ShouldRunTimerFunctions())
            {
                executionReport.RecordActivity(
                    "function-slot",
                    FunctionExecutionReportActivitySeverity.Warning,
                    "Execution skipped because the current slot is not production.");
                executionReport.RecordWarning("Execution skipped because the current slot is not production.");
                await this.CompleteReportAsync(executionReport, FunctionExecutionReportOutcome.Skipped, false, "Execution skipped because the current slot is not production.").ConfigureAwait(false);
                this.telemetryClient.TrackEvent(
                    "FunctionTimerSkippedNonProductionSlot",
                    new Dictionary<string, string>
                    {
                        { "FunctionName", nameof(GenerateSiteHealthReportFunction) },
                    });
                return;
            }

            try
            {
                SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
                if (!options.Enabled)
                {
                    log?.LogInformation("Site health report generation is disabled.");
                    executionReport.RecordActivity(
                        "site-health-disabled",
                        FunctionExecutionReportActivitySeverity.Information,
                        "Site health report generation is disabled by configuration.");
                    await this.CompleteReportAsync(executionReport, FunctionExecutionReportOutcome.Disabled, false, "Site health report generation is disabled.").ConfigureAwait(false);
                    return;
                }

                SiteHealthReportService service = new SiteHealthReportService(this.configuration);
                SiteHealthReport report = await service.GenerateAndStoreAsync(
                    "AzureFunction",
                    force: true,
                    slotKey: SiteHealthAppRoleResolver.ProductionSlotKey);
                this.telemetryClient.TrackEvent(
                    "SiteHealthReportPublished",
                    new Dictionary<string, string>
                    {
                        { "GeneratedAtUtc", report.GeneratedAtUtc.ToString("o") },
                        { "PeriodCount", report.Periods.Count.ToString() },
                    });
                executionReport.SetCounter("periodCount", report.Periods.Count);
                executionReport.SetCounter("recordsChanged", 1);
                executionReport.RecordActivity(
                    "site-health-report-published",
                    FunctionExecutionReportActivitySeverity.Information,
                    "The site health report was generated and stored.",
                    new Dictionary<string, long> { { "periodCount", report.Periods.Count } });
                await this.CompleteReportAsync(
                    executionReport,
                    FunctionExecutionReportOutcome.Succeeded,
                    businessDataChanged: true,
                    "The site health report was generated and stored.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                executionReport.RecordError("Site health report generation failed.");
                await this.CompleteReportAsync(
                    executionReport,
                    FunctionExecutionReportOutcome.Failed,
                    businessDataChanged: false,
                    "Site health report generation failed.").ConfigureAwait(false);
                this.telemetryClient.TrackException(
                    ex,
                    new Dictionary<string, string>
                    {
                        { "FunctionName", nameof(GenerateSiteHealthReportFunction) },
                    });
                throw;
            }
        }

        private async Task CompleteReportAsync(
            IFunctionExecutionReportExecution report,
            FunctionExecutionReportOutcome outcome,
            bool businessDataChanged,
            string message)
        {
            try
            {
                FunctionExecutionReportStorageResult result = await report.CompleteAsync(
                    outcome,
                    businessDataChanged,
                    message).ConfigureAwait(false);
                if (result.State != FunctionExecutionReportStorageState.Succeeded)
                {
                    this.telemetryClient.TrackEvent(
                        "FunctionExecutionReportPersistenceFailed",
                        new Dictionary<string, string>
                        {
                            { "FunctionName", nameof(GenerateSiteHealthReportFunction) },
                            { "State", result.State.ToString() },
                        });
                }
            }
            catch (Exception exception)
            {
                this.telemetryClient.TrackException(exception);
            }
        }

        private string GetCorrelationId(string invocationId)
        {
            return string.IsNullOrWhiteSpace(this.telemetryClient.Context.Operation.Id)
                ? invocationId
                : this.telemetryClient.Context.Operation.Id;
        }
    }
}
