// -----------------------------------------------------------------------
// <copyright file="SiteHealthLogAnalyticsClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Azure;
    using Azure.Identity;
    using Azure.Monitor.Query;
    using Azure.Monitor.Query.Models;

    /// <summary>
    /// Log Analytics query client for site health reporting.
    /// </summary>
    public class SiteHealthLogAnalyticsClient : ISiteHealthLogAnalyticsClient
    {
        private readonly LogsQueryClient queryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteHealthLogAnalyticsClient"/> class.
        /// </summary>
        public SiteHealthLogAnalyticsClient()
            : this(new LogsQueryClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                AdditionallyAllowedTenants = { "*" },
            })))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteHealthLogAnalyticsClient"/> class.
        /// </summary>
        /// <param name="queryClient">Logs query client.</param>
        public SiteHealthLogAnalyticsClient(LogsQueryClient queryClient)
        {
            this.queryClient = queryClient;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IReadOnlyDictionary<string, object>>> QueryAsync(
            string workspaceId,
            string query,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                throw new InvalidOperationException("SiteHealthReport:LogAnalyticsWorkspaceId is not configured.");
            }

            Response<LogsQueryResult> response = await this.queryClient.QueryWorkspaceAsync(
                workspaceId,
                query,
                new QueryTimeRange(TimeSpan.FromDays(8)),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            LogsTable table = response.Value.Table;
            if (table == null || table.Rows.Count == 0)
            {
                return Array.Empty<IReadOnlyDictionary<string, object>>();
            }

            IReadOnlyList<string> columnNames = table.Columns.Select(column => column.Name).ToList();
            List<IReadOnlyDictionary<string, object>> rows = new List<IReadOnlyDictionary<string, object>>(table.Rows.Count);
            foreach (IReadOnlyList<object> row in table.Rows)
            {
                Dictionary<string, object> item = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < columnNames.Count && i < row.Count; i++)
                {
                    item[columnNames[i]] = row[i];
                }

                rows.Add(item);
            }

            return rows;
        }
    }
}
