// -----------------------------------------------------------------------
// <copyright file="GenerateDonationReportFunction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Periodically generates static donation analytics pages and publishes them to blob storage.
    /// </summary>
    public class GenerateDonationReportFunction : MultiTenantFunction
    {
        private readonly DonationReportGenerationService reportGenerationService = new DonationReportGenerationService();

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateDonationReportFunction"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Application Insights configuration.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public GenerateDonationReportFunction(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
            : base(telemetryConfiguration, serviceProvider)
        {
            this.ExecuteFunction = this.GenerateReportAsync;
        }

        /// <summary>
        /// Timer entry point — runs once per day at 06:00 UTC.
        /// </summary>
        /// <param name="timer">Timer metadata.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A task.</returns>
        [Function("GenerateDonationReportFunction")]
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            try
            {
                await this.RunFunctionCore();
            }
            catch (Exception ex)
            {
                this.TelemetryClient.TrackException(
                    ex,
                    new Dictionary<string, string>()
                    {
                        { "FunctionName", nameof(GenerateDonationReportFunction) },
                    });
            }
        }

        private async Task GenerateReportAsync(IUnitOfWork unitOfWork, ApplicationDbContext applicationDbContext)
        {
            DonationReportGenerationResult result = await this.reportGenerationService.GenerateAndPublishAsync(
                this.Configuration,
                unitOfWork,
                applicationDbContext,
                new DonationReportGenerationRequest(),
                default);

            if (result.Skipped)
            {
                this.TelemetryClient.TrackTrace(result.Message);
                return;
            }

            if (!result.Succeeded)
            {
                this.TelemetryClient.TrackTrace(result.Message ?? "Donation report generation failed.");
                return;
            }

            this.TelemetryClient.TrackEvent(
                "DonationReportPublished",
                new Dictionary<string, string>()
                {
                    { "PagesUploaded", result.PagesUploaded.ToString() },
                    { "PaidAmount", result.TotalPaidAmount.ToString("F2") },
                    { "PaidCount", result.PaidDonationCount.ToString() },
                });
        }
    }
}
