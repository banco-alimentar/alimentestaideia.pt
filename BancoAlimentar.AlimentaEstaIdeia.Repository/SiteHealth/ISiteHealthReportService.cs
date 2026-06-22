// -----------------------------------------------------------------------
// <copyright file="ISiteHealthReportService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Builds and persists Application Insights site health reports.
    /// </summary>
    public interface ISiteHealthReportService
    {
        /// <summary>
        /// Loads the latest persisted report, if any.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Report or null.</returns>
        Task<SiteHealthReport> GetLatestReportAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets generation status from blob storage and in-process state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Status snapshot.</returns>
        Task<SiteHealthReportGenerationStatus> GetGenerationStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a fresh report and persists it.
        /// </summary>
        /// <param name="generatedBy">Source label (AdminPage, AzureFunction, etc.).</param>
        /// <param name="force">When false, respects SiteHealthReport:Enabled.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated report.</returns>
        Task<SiteHealthReport> GenerateAndStoreAsync(
            string generatedBy,
            bool force = false,
            CancellationToken cancellationToken = default);
    }
}
