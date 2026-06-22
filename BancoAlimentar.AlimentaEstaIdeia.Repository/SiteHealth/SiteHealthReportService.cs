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
        private string activeSlotKey;

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
        public async Task<SiteHealthReport> GetLatestReportAsync(CancellationToken cancellationToken = default)
        {
            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            SiteHealthResolvedSlot slot = SiteHealthAppRoleResolver.ResolveCurrentSlot(options);
            string connectionString = this.configuration["AzureStorage:ConnectionString"];

            SiteHealthReport report = await this.blobStore.LoadReportForSlotAsync(
                connectionString,
                options.BlobContainerName,
                slot.SlotKey,
                cancellationToken).ConfigureAwait(false);

            if (report == null &&
                string.Equals(slot.SlotKey, SiteHealthAppRoleResolver.ProductionSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                report = await this.blobStore.LoadReportAsync(
                    connectionString,
                    options.BlobContainerName,
                    SiteHealthReportPaths.LegacyReportBlobName,
                    cancellationToken).ConfigureAwait(false);
            }

            return SiteHealthReportNormalizer.Normalize(report);
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
            SiteHealthResolvedSlot slot = SiteHealthAppRoleResolver.ResolveCurrentSlot(options);
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            SiteHealthReportGenerationStatus persisted = await this.blobStore.LoadGenerationStatusForSlotAsync(
                connectionString,
                options.BlobContainerName,
                slot.SlotKey,
                cancellationToken).ConfigureAwait(false);

            if (persisted == null &&
                string.Equals(slot.SlotKey, SiteHealthAppRoleResolver.ProductionSlotKey, StringComparison.OrdinalIgnoreCase))
            {
                persisted = await this.blobStore.LoadGenerationStatusAsync(
                    connectionString,
                    options.BlobContainerName,
                    SiteHealthReportPaths.LegacyGenerationStatusBlobName,
                    cancellationToken).ConfigureAwait(false);
            }

            return persisted ?? inProcess;
        }

        /// <inheritdoc/>
        public async Task<SiteHealthReport> GenerateAndStoreAsync(
            string generatedBy,
            bool force = false,
            string slotKey = null,
            CancellationToken cancellationToken = default)
        {
            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            if (!options.Enabled && !force)
            {
                throw new InvalidOperationException("Site health report generation is disabled in configuration.");
            }

            SiteHealthResolvedSlot slot = string.IsNullOrWhiteSpace(slotKey)
                ? SiteHealthAppRoleResolver.ResolveCurrentSlot(options)
                : SiteHealthAppRoleResolver.ResolveFromSlotKey(slotKey, options);

            this.activeSlotKey = slot.SlotKey;

            if (!this.generationState.IsRunning)
            {
                this.generationState.TryStart();
            }

            try
            {
                this.generationState.SetProgress(5, $"Querying Application Insights ({slot.DisplayLabel}, 24h)…");
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);

                SiteHealthPeriodReport last24Hours = await this.BuildPeriodReportAsync(
                    options,
                    slot,
                    "24h",
                    cancellationToken).ConfigureAwait(false);

                this.generationState.SetProgress(45, $"Querying Application Insights ({slot.DisplayLabel}, 7d)…");
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);

                SiteHealthPeriodReport last7Days = await this.BuildPeriodReportAsync(
                    options,
                    slot,
                    "7d",
                    cancellationToken).ConfigureAwait(false);

                this.generationState.SetProgress(85, "Saving report…");
                await this.PersistStatusAsync(cancellationToken).ConfigureAwait(false);

                SiteHealthReport report = new SiteHealthReport
                {
                    GeneratedAtUtc = DateTime.UtcNow,
                    GeneratedBy = generatedBy,
                    WorkspaceId = options.LogAnalyticsWorkspaceId,
                    SlotKey = slot.SlotKey,
                    SlotLabel = slot.DisplayLabel,
                    AppRoleName = slot.AppRoleName,
                    Periods = new List<SiteHealthPeriodReport> { last24Hours, last7Days },
                };

                string connectionString = this.configuration["AzureStorage:ConnectionString"];
                await this.blobStore.SaveReportForSlotAsync(
                    connectionString,
                    options.BlobContainerName,
                    slot.SlotKey,
                    report,
                    cancellationToken).ConfigureAwait(false);

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
            finally
            {
                this.activeSlotKey = null;
            }
        }

        private async Task<SiteHealthPeriodReport> BuildPeriodReportAsync(
            SiteHealthReportOptions options,
            SiteHealthResolvedSlot slot,
            string window,
            CancellationToken cancellationToken)
        {
            string workspaceId = options.LogAnalyticsWorkspaceId;
            string targetRole = slot.AppRoleName;

            long requestCount = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.RequestCount(window, targetRole),
                "RequestCount",
                cancellationToken).ConfigureAwait(false);

            long failedRequestCount = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.FailedRequestCount(window, targetRole),
                "FailedRequestCount",
                cancellationToken).ConfigureAwait(false);

            long exceptionCount = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.ExceptionCount(window, targetRole),
                "ExceptionCount",
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> eventCounts = await this.EventCountsAsync(
                workspaceId,
                SiteHealthReportQueries.EventCountsByName(window, targetRole),
                cancellationToken).ConfigureAwait(false);

            Dictionary<string, long> rejectionReasons = await this.RejectionReasonsAsync(
                workspaceId,
                SiteHealthReportQueries.EasypayRejectionReasons(window, targetRole),
                cancellationToken).ConfigureAwait(false);

            long easypayDistinctKeys = await this.ScalarLongAsync(
                workspaceId,
                SiteHealthReportQueries.EasypayLookupDistinctKeys(window, targetRole),
                "DistinctKeys",
                cancellationToken).ConfigureAwait(false);

            List<SiteHealthReportIssue> issues = new List<SiteHealthReportIssue>();

            foreach (KeyValuePair<string, long> pair in rejectionReasons.Where(p => p.Value > 0))
            {
                string code = $"EasypayWebhookRejected:{pair.Key}";
                issues.Add(SiteHealthIssueCatalog.CreateIssue(code, pair.Value, null, new List<string>
                {
                    $"Reason: {pair.Key} ({pair.Value} events)",
                }));
            }

            foreach (KeyValuePair<string, long> pair in eventCounts.Where(p => p.Value > 0))
            {
                if (string.Equals(pair.Key, "EasypayWebhookRejected", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                long? distinct = string.Equals(pair.Key, "EasypayApiLookupFailed", StringComparison.OrdinalIgnoreCase)
                    ? easypayDistinctKeys
                    : null;

                issues.Add(SiteHealthIssueCatalog.CreateIssue(pair.Key, pair.Value, distinct));
            }

            if (failedRequestCount > 0)
            {
                issues.Add(SiteHealthIssueCatalog.CreateIssue("FailedHttpRequests", failedRequestCount));
            }

            if (exceptionCount > 0)
            {
                issues.Add(SiteHealthIssueCatalog.CreateIssue("UnhandledExceptions", exceptionCount));
            }

            issues = issues
                .OrderByDescending(i => i.Severity)
                .ThenByDescending(i => i.Count)
                .ToList();

            int criticalCount = issues.Count(i => i.Severity == SiteHealthReportSeverity.Critical && i.Count > 0);
            int warningCount = issues.Count(i => i.Severity == SiteHealthReportSeverity.Warning && i.Count > 0);
            SiteHealthOverallStatus overall = SiteHealthIssueCatalog.ComputeOverallStatus(
                issues,
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
                ProductionIssues = issues,
                InformationalIssues = new List<SiteHealthReportIssue>(),
            };
        }

        private async Task PersistStatusAsync(CancellationToken cancellationToken)
        {
            SiteHealthReportOptions options = SiteHealthReportConfiguration.ReadOptions(this.configuration);
            string connectionString = this.configuration["AzureStorage:ConnectionString"];
            string slotKey = this.activeSlotKey ?? SiteHealthAppRoleResolver.ResolveCurrentSlot(options).SlotKey;
            await this.blobStore.SaveGenerationStatusForSlotAsync(
                connectionString,
                options.BlobContainerName,
                slotKey,
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
