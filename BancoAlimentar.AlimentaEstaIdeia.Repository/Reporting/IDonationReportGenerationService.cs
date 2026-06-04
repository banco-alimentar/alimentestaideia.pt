// -----------------------------------------------------------------------
// <copyright file="IDonationReportGenerationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Builds and publishes static donation analytics reports.
    /// </summary>
    public interface IDonationReportGenerationService
    {
        /// <summary>
        /// Generates HTML report pages and publishes them using tenant configuration.
        /// </summary>
        /// <param name="request">Generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generation outcome.</returns>
        Task<DonationReportGenerationResult> GenerateAndPublishAsync(
            DonationReportGenerationRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates HTML report pages and publishes them (explicit dependencies for Azure Functions).
        /// </summary>
        /// <param name="configuration">Tenant configuration.</param>
        /// <param name="unitOfWork">Unit of work.</param>
        /// <param name="applicationDbContext">Database context.</param>
        /// <param name="request">Generation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generation outcome.</returns>
        Task<DonationReportGenerationResult> GenerateAndPublishAsync(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ApplicationDbContext applicationDbContext,
            DonationReportGenerationRequest request,
            CancellationToken cancellationToken = default);
    }
}
