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

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateSiteHealthReportFunction"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Application Insights configuration.</param>
        /// <param name="configuration">Function configuration.</param>
        public GenerateSiteHealthReportFunction(TelemetryConfiguration telemetryConfiguration, IConfiguration configuration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.configuration = configuration;
        }

        /// <summary>
        /// Timer entry point — runs daily at 07:00 UTC (after donation report).
        /// </summary>
        /// <param name="timer">Timer metadata.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A task.</returns>
        [Function("GenerateSiteHealthReportFunction")]
        public async Task Run([TimerTrigger("0 0 7 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            if (!FunctionSlotExecution.ShouldRunTimerFunctions())
            {
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
                    log.LogInformation("Site health report generation is disabled.");
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
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(
                    ex,
                    new Dictionary<string, string>
                    {
                        { "FunctionName", nameof(GenerateSiteHealthReportFunction) },
                    });
                throw;
            }
        }
    }
}
