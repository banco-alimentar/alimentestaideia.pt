// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Builds site health reports from Log Analytics and persists them to blob storage.
    /// </summary>
    public class SiteHealthReportService : ISiteHealthReportService
    {
        private readonly IConfiguration configuration;
        private readonly ISiteHealthLogAnalyticsClient logAnalyticsClient;
        private readonly SiteHealthReportBlobStore blobStore;
        private readonly SiteHealthReportGenerationState generationState;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteHealthReportService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logAnalyticsClient">Log Analytics client.</param>
        /// <param name="blobStore">Blob persistence.</param>
        /// <param name="generationState">In-process generation state.</param>
        public SiteHealthReportService(
            IConfiguration configuration,
            ISiteHealthLogAnalyticsClient logAnalyticsClient,
            SiteHealthReportBlobStore blobStore,
            SiteHealthReportGenerationState generationState)
        {
            this.configuration = configuration;
            this.logAnalyticsClient = logAnalyticsClient;
            this.blobStore = blobStore;
            this.generationState = generationState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteHealthReportService"/> class for Azure Functions.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public SiteHealthReportService(IConfiguration configuration)
            : this(
                configuration,
                new SiteHealthLogAnalyticsClient(),
                new SiteHealthReportBlobStore(),
                new SiteHealthReportGenerationState())
        {
        }

        /// <inheritdoc/>
        public Task<SiteHealthReport> GetLatestReportAsync(CancellationToken cancellationToken = default)
        {
            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            return this.blobStore.LoadReportAsync(connectionString, options.BlobContainerName, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<SiteHealthReportGenerationStatus> GetGenerationStatusAsync(CancellationToken cancellationToken = default)
        {
            SiteHealthReportGenerationStatus inProcess = this.generationState.ToStatus();
            if (inProcess.IsRunning)
            {
                return inProcess;
            }

            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            SiteHealthReportGenerationStatus persisted = await this.blobStore.LoadGenerationStatusAsync(
                connectionString,
                options.BlobContainerName,
                cancellationToken).ConfigureAwait(false);

            return persisted ?? inProcess;
        }

        /// <inheritdoc/>
        public async Task<SiteHealthReport> GenerateAndStoreAsync(
            string generatedBy,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            if (!options.Enabled && !force)
            {
                throw new InvalidOperationException("Site health report generation is disabled in configuration.");
            }

            if (!this.generationState.IsRunning)
            {
                this.generationState.TryStart();
            }

            try
            {
                this.generationState.SetProgress(5, "Querying Application Insights (24h)…");
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);

                SiteHealthPeriodReport last24Hours = await this.BuildPeriodReportAsync(
                    options,
                    "24h",
                    cancellationToken).ConfigureAwait(false);

                this.generationState.SetProgress(45, "Querying Application Insights (7d)…");
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);

                SiteHealthPeriodReport last7Days = await this.BuildPeriodReportAsync(
                    options,
                    "7d",
                    cancellationToken).ConfigureAwait(false);

                this.generationState.SetProgress(85, "Saving report…");
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);

                SiteHealthReport report = new SiteHealthReport
                {
                    GeneratedAtUtc = DateTime.UtcNow,
                    GeneratedBy = generatedBy,
                    WorkspaceId = options.LogAnalyticsWorkspaceId,
                    ProductionAppRoleName = options.ProductionAppRoleName,
                    Periods = new List<SiteHealthPeriodReport> { last24Hours, last7Days },
                };

                string connectionString = this.configuration["AzureStorage:ConnectionString"];
                await this.blobStore.SaveReportAsync(connectionString, options.BlobContainerName, report, cancellationToken)
                    .ConfigureAwait(false);

                this.generationState.CompleteSuccess(report.GeneratedAtUtc);
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);
                return report;
            }
            catch (Exception ex)
            {
                this.generationState.CompleteFailure(ex.Message);
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<SiteHealthPeriodReport> BuildPeriodReportAsync(
            SiteHealthReportOptions options,
            string window,
            CancellationToken cancellationToken)
        {
            string workspaceId = options.LogAnalyticsWorkspaceId;
            string productionRole = options.ProductionAppRoleName;
            string developerRole = options.DeveloperAppRoleName;

            long requestCount = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.RequestCount(window, productionRole),
                "RequestCount",
                cancellationToken).ConfigureAwait(false);

            long failedRequestCount = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.FailedRequestCount(window, productionRole),
                "FailedRequestCount",
                cancellationToken).ConfigureAwait(false);

            long exceptionCount = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.ExceptionCount(window, productionRole),
                "ExceptionCount",
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> productionCounts = await this.EventCountsAsync(
                workspaceId,
                SiteHealthReportQueries.EventCountsByName(window, productionRole),
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> rejectionReasons = await this.RejectionReasonsAsync(
                workspaceId,
                SiteHealthReportQueries.EasypayRejectionReasons(window, productionRole),
                cancellationToken).ConfigureAwait(false);

            long easypayDistinctKeys = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.EasypayLookupDistinctKeys(window, productionRole),
                "DistinctKeys",
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> developerCounts = await this.EventCountsAsync(
                workspaceId,
                SiteHealthReportQueries.EventCountsByName(window, developerRole),
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> ciCounts = await this.EventCountsAsync(
                workspaceId,
                SiteHealthReportQueries.EventCountsByName(window, string.Empty),
                cancellationToken).ConfigureAwait(false);

            List<SiteHealthReportIssue> productionIssues = new List<SiteHealthReportIssue>();

            foreach (KeyValuePair<string, long> pair in rejectionReasons.Where(p => p.Value > 0))
            {
                string code = $"EasypayWebhookRejected:{pair.Key}";
                productionIssues.Add(SiteHealthIssueCatalog.CreateIssue(code, pair.Value, null, new List<string>
                {
                    $"Reason: {pair.Key} ({pair.Value} events)",
                }));
            }

            foreach (KeyValuePair<string, long> pair in productionCounts.Where(p => p.Value > 0))
            {
                if (string.Equals(pair.Key, "EasypayWebhookRejected", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                long? distinct = string.Equals(pair.Key, "EasypayApiLookupFailed", StringComparison.OrdinalIgnoreCase)
                    ? easypayDistinctKeys
                    : null;

                productionIssues.Add(SiteHealthIssueCatalog.CreateIssue(pair.Key, pair.Value, distinct));
            }

            if (failedRequestCount > 0)
            {
                productionIssues.Add(SiteHealthIssueCatalog.CreateIssue("FailedHttpRequests", failedRequestCount));
            }

            if (exceptionCount > 0)
            {
                productionIssues.Add(SiteHealthIssueCatalog.CreateIssue("UnhandledExceptions", exceptionCount));
            }

            productionIssues = productionIssues
                .OrderByDescending(i => i.Severity)
                .ThenByDescending(i => i.Count)
                .ToList();

            List<SiteHealthReportIssue> informationalIssues = new List<SiteHealthReportIssue>();
            foreach (KeyValuePair<string, long> pair in developerCounts.Where(p => p.Value > 0))
            {
                SiteHealthReportIssue issue = SiteHealthIssueCatalog.CreateIssue(pair.Key, pair.Value);
                issue.Title += " (developer slot)";
                informationalIssues.Add(issue);
            }

            foreach (KeyValuePair<string, long> pair in ciCounts.Where(p => p.Value > 0))
            {
                SiteHealthReportIssue issue = SiteHealthIssueCatalog.CreateIssue(pair.Key, pair.Value);
                issue.Title += " (CI / unscoped)";
                informationalIssues.Add(issue);
            }

            int criticalCount = productionIssues.Count(i => i.Severity == SiteHealthReportSeverity.Critical && i.Count > 0);
            int warningCount = productionIssues.Count(i => i.Severity == SiteHealthReportSeverity.Warning && i.Count > 0);
            SiteHealthOverallStatus overall = SiteHealthIssueCatalog.ComputeOverallStatus(
                productionIssues,
                exceptionCount,
                failedRequestCount);

            return new SiteHealthPeriodReport
            {
                WindowLabel = window,
                OverallStatus = overall,
                Summary = SiteHealthIssueCatalog.BuildSummary(overall, criticalCount, warningCount),
                RequestCount = requestCount,
                FailedRequestCount = failedRequestCount,
                ExceptionCount = exceptionCount,
                ProductionIssues = productionIssues,
                InformationalIssues = informationalIssues,
            };
        }

        private async Task PersistStatusAsync(CancellationToken cancellationToken)
        {
            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            await this.blobStore.SaveGenerationStatusAsync(
                connectionString,
                options.BlobContainerName,
                this.generationState.ToStatus(),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<Dictionary<string, long>> EventCountsAsync(
            string workspaceId,
            string query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<IReadOnlyDictionary<string, object>> rows = await this.logAnalyticsClient.QueryAsync(
                workspaceId,
                query,
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (IReadOnlyDictionary<string, object> row in rows)
            {
                string name = SiteHealthQueryRowParser.GetString(row, "Name");
                long count = SiteHealthQueryRowParser.GetLong(row, "count_");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result[name] = count;
                }
            }

            return result;
        }

        private async Task<Dictionary<string, long>> RejectionReasonsAsync(
            string workspaceId,
            string query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<IReadOnlyDictionary<string, object>> rows = await this.logAnalyticsClient.QueryAsync(
                workspaceId,
                query,
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (IReadOnlyDictionary<string, object> row in rows)
            {
                string reason = SiteHealthQueryRowParser.GetString(row, "Reason");
                long count = SiteHealthQueryRowParser.GetLong(row, "count_");
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    result[reason] = count;
                }
            }

            return result;
        }

        private async Task<long> ScalarLongAsync(
            string workspaceId,
            string query,
            string columnName,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<IReadOnlyDictionary<string, object>> rows = await this.logAnalyticsClient.QueryAsync(
                workspaceId,
                query,
                cancellationToken).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                return 0;
            }

            return SiteHealthQueryRowParser.GetLong(rows[0], columnName);
        }
    }
}
