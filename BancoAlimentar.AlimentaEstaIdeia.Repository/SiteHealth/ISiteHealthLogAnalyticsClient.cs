// -----------------------------------------------------------------------
// <copyright file="ISiteHealthLogAnalyticsClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Executes Kusto queries against the Application Insights Log Analytics workspace.
    /// </summary>
    public interface ISiteHealthLogAnalyticsClient
    {
        /// <summary>
        /// Runs a query and returns rows as dictionaries keyed by column name.
        /// </summary>
        /// <param name="workspaceId">Log Analytics workspace customer id.</param>
        /// <param name="query">Kusto query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result rows.</returns>
        Task<IReadOnlyList<IReadOnlyDictionary<string, object>>> QueryAsync(
            string workspaceId,
            string query,
            CancellationToken cancellationToken = default);
    }
}
