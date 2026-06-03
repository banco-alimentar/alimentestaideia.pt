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
    using BancoAlimentar.AlimentaEstaIdeia.Function.Reporting;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Periodically generates static donation analytics pages and publishes them to blob storage.
    /// </summary>
    public class GenerateDonationReportFunction : MultiTenantFunction
    {
        private readonly DonationReportBlobPublisher blobPublisher = new DonationReportBlobPublisher();

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
            DonationReportOptions options = DonationReportConfiguration.ReadOptions(this.Configuration);
            if (!options.Enabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(options.BlobContainerName))
            {
                this.TelemetryClient.TrackTrace("Donation report skipped: blob container name is not configured.");
                return;
            }

            string connectionString = this.Configuration["AzureStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                this.TelemetryClient.TrackTrace("Donation report skipped: AzureStorage:ConnectionString missing.");
                return;
            }

            string tenantName = this.Configuration["Tenant:DisplayName"]
                ?? this.Configuration["Tenant:Name"]
                ?? "Alimente esta ideia";

            DonationReportRepository reportRepository = new DonationReportRepository(applicationDbContext, unitOfWork.Donation);
            DonationReportSnapshot snapshot = await reportRepository.BuildSnapshotAsync(tenantName);

            IReadOnlyDictionary<string, string> pages = DonationReportHtmlGenerator.GenerateAllPages(snapshot, options.SiteTitle);
            int uploaded = await this.blobPublisher.PublishAsync(
                connectionString,
                options.BlobContainerName,
                options.BlobPrefix,
                pages);

            this.TelemetryClient.TrackEvent(
                "DonationReportPublished",
                new Dictionary<string, string>()
                {
                    { "BlobContainer", options.BlobContainerName },
                    { "BlobPrefix", options.BlobPrefix ?? string.Empty },
                    { "PagesUploaded", uploaded.ToString() },
                    { "PaidAmount", snapshot.Summary.TotalPaidAmount.ToString("F2") },
                    { "PaidCount", snapshot.Summary.PaidDonationCount.ToString() },
                });
        }
    }
}
