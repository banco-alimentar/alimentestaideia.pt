// -----------------------------------------------------------------------
// <copyright file="MultiTenantFunction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>Base class for all Azure Functions.</summary>
    public class MultiTenantFunction
    {
        private readonly IServiceProvider serviceProvider;
        private readonly FunctionExecutionReportCoordinatorFactory reportCoordinatorFactory;
        private readonly TelemetryClient telemetryClient;
        private IConfiguration configuration;

        /// <summary>Initializes a new instance of the <see cref="MultiTenantFunction"/> class.</summary>
        public MultiTenantFunction(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.serviceProvider = serviceProvider;
            this.reportCoordinatorFactory = serviceProvider.GetService<FunctionExecutionReportCoordinatorFactory>()
                ?? new FunctionExecutionReportCoordinatorFactory();
        }

        /// <summary>Gets or sets the legacy function delegate used by existing callers and tests.</summary>
        public Func<IUnitOfWork, ApplicationDbContext, Task> ExecuteFunction { get; set; }

        /// <summary>Gets or sets the reporter-aware function delegate used by timer execution.</summary>
        public Func<IUnitOfWork, ApplicationDbContext, IFunctionExecutionReportExecution, Task> ExecuteFunctionWithReport { get; set; }

        /// <summary>Gets the service provider.</summary>
        public IServiceProvider ServiceProvider => this.serviceProvider;

        /// <summary>Gets the telemetry client.</summary>
        public TelemetryClient TelemetryClient => this.telemetryClient;

        /// <summary>Gets the current tenant configuration.</summary>
        public IConfiguration Configuration => this.configuration;

        /// <summary>Gets the stable report function key.</summary>
        protected virtual string ReportFunctionKey => this.GetType().Name;

        /// <summary>Loads configuration and executes the function for every tenant.</summary>
        /// <returns>A task object to monitor progress.</returns>
        public Task RunManualAsync(FunctionExecutionCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return this.RunFunctionCore(command.GetTriggerType(), command.CommandId, command.CorrelationId);
        }

        /// <summary>Loads configuration and executes the function for every tenant.</summary>
        /// <param name="triggerType">Report trigger type.</param>
        /// <param name="invocationId">Optional invocation identifier.</param>
        /// <param name="correlationId">Optional correlation identifier.</param>
        /// <returns>A task object to monitor progress.</returns>
        public async Task RunFunctionCore(
            string triggerType = "TimerTrigger",
            string invocationId = null,
            string correlationId = null)
        {
            invocationId ??= Guid.NewGuid().ToString("N");
            correlationId ??= this.GetCorrelationId(invocationId);
            if (!FunctionSlotExecution.ShouldRunTimerFunctions())
            {
                await this.RecordGlobalOutcomeAsync(
                    invocationId,
                    FunctionExecutionReportOutcome.Skipped,
                    "Execution skipped because the current slot is not production.",
                    FunctionExecutionReportActivitySeverity.Warning,
                    triggerType,
                    correlationId).ConfigureAwait(false);
                this.TelemetryClient.TrackEvent(
                    "FunctionTimerSkippedNonProductionSlot",
                    new Dictionary<string, string>
                    {
                        { "SlotName", Environment.GetEnvironmentVariable(FunctionSlotExecution.WebsiteSlotNameVariable) ?? string.Empty },
                    });
                return;
            }

            IFunctionExecutionReportExecution infrastructureReport = null;
            try
            {
                infrastructureReport = this.reportCoordinatorFactory.Create(this.ServiceProvider.GetService<IConfiguration>()).Begin(
                    this.ReportFunctionKey,
                    new FunctionExecutionReportScope(FunctionSlotExecution.GetSlotKey(), "global"),
                    invocationId,
                    this.ReportFunctionKey,
                    triggerType: triggerType,
                    environment: GetReportEnvironment(),
                    correlationId: correlationId);
                InfrastructureDbContext infrastructureDbContext = this.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
                List<Tenant> allTenants = infrastructureDbContext.Tenants.ToList();
                infrastructureReport.RecordActivity(
                    "tenant-discovery",
                    FunctionExecutionReportActivitySeverity.Information,
                    $"Discovered {allTenants.Count} tenant configurations.",
                    new Dictionary<string, long> { { "tenantCount", allTenants.Count } });

                IKeyVaultConfigurationManager keyVaultConfigurationManager = this.ServiceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
                keyVaultConfigurationManager.LoadTenantConfiguration();
                TenantExecutionSummary tenantSummary = await this.ExecuteTenantsAsync(allTenants, keyVaultConfigurationManager, invocationId, triggerType, correlationId).ConfigureAwait(false);
                infrastructureReport.SetCounter("tenantsDiscovered", tenantSummary.Discovered);
                infrastructureReport.SetCounter("tenantsProcessed", tenantSummary.Processed);
                infrastructureReport.SetCounter("tenantsSucceeded", tenantSummary.Succeeded);
                infrastructureReport.SetCounter("tenantsFailed", tenantSummary.Failed);
                infrastructureReport.SetCounter("tenantsSkipped", tenantSummary.Skipped);
                infrastructureReport.SetCounter("tenantFailures", tenantSummary.Failed);
                FunctionExecutionReportOutcome outcome = TenantExecutionOutcomeAggregator.GetOverallOutcome(
                    tenantSummary.Succeeded,
                    tenantSummary.Failed,
                    tenantSummary.Skipped,
                    tenantSummary.Partial);
                string summaryMessage = tenantSummary.Failed == 0
                    ? "Tenant discovery and configuration loading completed."
                    : $"Tenant processing completed with {tenantSummary.Failed} tenant failure(s).";
                await this.CompleteReportAsync(
                    infrastructureReport,
                    outcome,
                    businessDataChanged: false,
                    message: summaryMessage).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (infrastructureReport != null)
                {
                    infrastructureReport.RecordError("Tenant infrastructure setup failed.");
                    await this.CompleteReportAsync(
                        infrastructureReport,
                        FunctionExecutionReportOutcome.Failed,
                        businessDataChanged: false,
                        "Tenant infrastructure setup failed.").ConfigureAwait(false);
                }

                this.TelemetryClient.TrackException(exception);
                throw;
            }
        }

        private static string GetReportEnvironment()
        {
            return Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Unknown";
        }

        private async Task<TenantExecutionSummary> ExecuteTenantsAsync(
            IReadOnlyCollection<Tenant> allTenants,
            IKeyVaultConfigurationManager keyVaultConfigurationManager,
            string invocationId,
            string triggerType,
            string correlationId)
        {
            var summary = new TenantExecutionSummary { Discovered = allTenants.Count };
            IConfiguration reportConfiguration = this.ServiceProvider.GetService<IConfiguration>();
            foreach (Tenant tenant in allTenants)
            {
                summary.Processed++;
                IFunctionExecutionReportExecution report = null;
                try
                {
                    await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id, TenantDevelopmentOptions.ProductionOptions).ConfigureAwait(false);
                    Dictionary<string, string> tenantConfiguration = new Dictionary<string, string>(
                        keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id) ?? new Dictionary<string, string>());
                    tenantConfiguration["Tenant:NormalizedName"] = tenant.NormalizedName;
                    tenantConfiguration["Tenant:Name"] = tenant.Name;
                    var memoryConfigurationSource = new MemoryConfigurationSource
                    {
                        InitialData = tenantConfiguration,
                    };
                    var configurationBuilder = new ConfigurationBuilder();
                    configurationBuilder.Add(memoryConfigurationSource);
                    this.configuration = configurationBuilder.Build();

                    report = this.reportCoordinatorFactory.Create(reportConfiguration).Begin(
                        this.ReportFunctionKey,
                        new FunctionExecutionReportScope(FunctionSlotExecution.GetSlotKey(), tenant.NormalizedName),
                        invocationId,
                        this.ReportFunctionKey,
                        triggerType: triggerType,
                        environment: GetReportEnvironment(),
                        correlationId: correlationId);
                    report.RecordActivity(
                        "tenant-configuration-loaded",
                        FunctionExecutionReportActivitySeverity.Information,
                        "Tenant configuration loaded and database execution initialized.",
                        new Dictionary<string, long> { { "tenantIdAvailable", 1 } });
                    report.RecordActivity(
                        "function-started",
                        FunctionExecutionReportActivitySeverity.Information,
                        "Tenant function execution started.");

                    var config = FunctionInitializer.GetUnitOfWork(this.TelemetryClient, this.configuration);
                    if (this.ExecuteFunctionWithReport != null)
                    {
                        await this.ExecuteFunctionWithReport(config.UnitOfWork, config.ApplicationDbContext, report).ConfigureAwait(false);
                    }
                    else if (this.ExecuteFunction != null)
                    {
                        await this.ExecuteFunction(config.UnitOfWork, config.ApplicationDbContext).ConfigureAwait(false);
                    }

                    report.RecordActivity(
                        "function-completed",
                        FunctionExecutionReportActivitySeverity.Information,
                        "Tenant function execution completed.");
                    this.TelemetryClient.TrackEvent(
                        "FunctionCoreExecuted",
                        new Dictionary<string, string>
                        {
                            { "TenantId", tenant.Id.ToString() },
                            { "Name", tenant.Name },
                        });
                    FunctionExecutionReportStorageResult completion = await this.CompleteReportAsync(
                        report,
                        FunctionExecutionReportOutcome.Succeeded,
                        businessDataChanged: false,
                        "Tenant function execution completed.").ConfigureAwait(false);
                    switch (completion.BusinessOutcome)
                    {
                        case FunctionExecutionReportOutcome.Succeeded:
                            summary.Succeeded++;
                            break;
                        case FunctionExecutionReportOutcome.Skipped:
                        case FunctionExecutionReportOutcome.Disabled:
                            summary.Skipped++;
                            break;
                        case FunctionExecutionReportOutcome.Failed:
                            summary.Failed++;
                            break;
                        default:
                            summary.Partial++;
                            break;
                    }
                }
                catch (Exception exception)
                {
                    summary.Failed++;
                    this.TelemetryClient.TrackException(exception);
                    if (report == null)
                    {
                        try
                        {
                            report = this.reportCoordinatorFactory.Create(reportConfiguration).Begin(
                                this.ReportFunctionKey,
                                new FunctionExecutionReportScope(FunctionSlotExecution.GetSlotKey(), tenant.NormalizedName),
                                invocationId,
                                this.ReportFunctionKey,
                                triggerType: triggerType,
                                environment: GetReportEnvironment(),
                                correlationId: correlationId);
                        }
                        catch (Exception reportException)
                        {
                            this.TelemetryClient.TrackException(reportException);
                        }
                    }

                    if (report != null)
                    {
                        report.RecordError("Tenant function execution failed.");
                        await this.CompleteReportAsync(
                            report,
                            FunctionExecutionReportOutcome.Failed,
                            businessDataChanged: false,
                            "Tenant function execution failed.").ConfigureAwait(false);
                    }
                }
            }

            return summary;
        }

        private async Task RecordGlobalOutcomeAsync(
            string invocationId,
            FunctionExecutionReportOutcome outcome,
            string message,
            FunctionExecutionReportActivitySeverity severity,
            string triggerType,
            string correlationId)
        {
            IFunctionExecutionReportExecution report = this.reportCoordinatorFactory.Create(this.ServiceProvider.GetService<IConfiguration>()).Begin(
                this.ReportFunctionKey,
                new FunctionExecutionReportScope(FunctionSlotExecution.GetSlotKey(), "global"),
                invocationId,
                this.ReportFunctionKey,
                triggerType: triggerType,
                environment: GetReportEnvironment(),
                correlationId: correlationId);
            report.RecordActivity("function-slot", severity, message);
            if (severity == FunctionExecutionReportActivitySeverity.Warning)
            {
                report.RecordWarning(message);
            }

            await this.CompleteReportAsync(report, outcome, businessDataChanged: false, message).ConfigureAwait(false);
        }

        private async Task<FunctionExecutionReportStorageResult> CompleteReportAsync(
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
                    this.TelemetryClient.TrackEvent(
                        "FunctionExecutionReportPersistenceFailed",
                        new Dictionary<string, string>
                        {
                            { "FunctionName", this.ReportFunctionKey },
                            { "State", result.State.ToString() },
                        });
                }

                return result;
            }
            catch (Exception exception)
            {
                this.TelemetryClient.TrackException(exception);
                return new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Unavailable,
                    BusinessOutcome = outcome,
                    Diagnostic = "Execution report completion failed.",
                    Exception = exception,
                };
            }
        }

        private string GetCorrelationId(string invocationId)
        {
            return string.IsNullOrWhiteSpace(this.TelemetryClient.Context.Operation.Id)
                ? invocationId
                : this.TelemetryClient.Context.Operation.Id;
        }

        private sealed class TenantExecutionSummary
        {
            public int Discovered { get; set; }

            public int Processed { get; set; }

            public int Succeeded { get; set; }

            public int Failed { get; set; }

            public int Skipped { get; set; }

            public int Partial { get; set; }
        }
    }
}
