// -----------------------------------------------------------------------
// <copyright file="DonationReportGenerationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Builds donation report snapshots, renders HTML, and publishes to blob and/or local storage.
    /// </summary>
    public class DonationReportGenerationService : IDonationReportGenerationService
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork unitOfWork;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
        private readonly DonationReportBlobPublisher blobPublisher = new DonationReportBlobPublisher();
        private readonly DonationReportLocalPublisher localPublisher = new DonationReportLocalPublisher();

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationReportGenerationService"/> class
        /// for explicit dependency invocation (e.g. Azure Functions).
        /// </summary>
        public DonationReportGenerationService()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationReportGenerationService"/> class.
        /// </summary>
        /// <param name="configuration">Tenant configuration.</param>
        /// <param name="unitOfWork">Unit of work.</param>
        /// <param name="dbContextFactory">Database context factory.</param>
        public DonationReportGenerationService(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            this.configuration = configuration;
            this.unitOfWork = unitOfWork;
            this.dbContextFactory = dbContextFactory;
        }

        /// <inheritdoc/>
        public async Task<DonationReportGenerationResult> GenerateAndPublishAsync(
            DonationReportGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            await using ApplicationDbContext applicationDbContext =
                await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await this.GenerateAndPublishAsync(
                this.configuration,
                this.unitOfWork,
                applicationDbContext,
                request,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<DonationReportGenerationResult> GenerateAndPublishAsync(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ApplicationDbContext applicationDbContext,
            DonationReportGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            DonationReportOptions options = DonationReportConfiguration.ReadOptions(configuration);
            if (!options.Enabled && (request == null || !request.Force))
            {
                return new DonationReportGenerationResult
                {
                    Skipped = true,
                    Message = "Donation report is disabled for this tenant (DonationReport:Enabled).",
                };
            }

            string blobContainerName = ResolveBlobContainerName(options, request);

            if (string.IsNullOrWhiteSpace(blobContainerName)
                && string.IsNullOrWhiteSpace(request?.LocalOutputDirectory))
            {
                return new DonationReportGenerationResult
                {
                    Message = "Blob container name is not configured and no local output directory was specified.",
                };
            }

            string connectionString = configuration["AzureStorage:ConnectionString"];
            bool publishToBlob = !string.IsNullOrWhiteSpace(connectionString)
                && !string.IsNullOrWhiteSpace(blobContainerName);

            if (!publishToBlob && string.IsNullOrWhiteSpace(request?.LocalOutputDirectory))
            {
                return new DonationReportGenerationResult
                {
                    Message = "AzureStorage:ConnectionString is missing and no local output directory was specified.",
                };
            }

            string tenantName = configuration["Tenant:DisplayName"]
                ?? configuration["Tenant:Name"]
                ?? "Alimente esta ideia";

            DonationReportRepository reportRepository = new DonationReportRepository(applicationDbContext, unitOfWork.Donation);
            DonationReportSnapshot snapshot = await reportRepository.BuildSnapshotAsync(tenantName);

            IReadOnlyDictionary<string, string> pages = DonationReportHtmlGenerator.GenerateAllPages(snapshot, options.SiteTitle);

            int pagesUploaded = 0;
            if (publishToBlob)
            {
                pagesUploaded = await this.blobPublisher.PublishAsync(
                    connectionString,
                    blobContainerName,
                    options.BlobPrefix,
                    pages,
                    cancellationToken);
            }

            int pagesWrittenLocally = 0;
            string localPublishWarning = null;
            if (!string.IsNullOrWhiteSpace(request?.LocalOutputDirectory))
            {
                try
                {
                    pagesWrittenLocally = this.localPublisher.PublishToDirectory(request.LocalOutputDirectory, pages);
                }
                catch (Exception ex)
                {
                    if (pagesUploaded > 0)
                    {
                        localPublishWarning = "Report was published to blob storage, but local file output failed: " + ex.Message;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (pagesUploaded == 0 && pagesWrittenLocally == 0)
            {
                return new DonationReportGenerationResult
                {
                    Message = localPublishWarning ?? "No report files were published.",
                };
            }

            return new DonationReportGenerationResult
            {
                Succeeded = true,
                Message = localPublishWarning ?? BuildSuccessMessage(pagesUploaded, pagesWrittenLocally),
                PagesUploaded = pagesUploaded,
                PagesWrittenLocally = pagesWrittenLocally,
                TotalPaidAmount = snapshot.Summary.TotalPaidAmount,
                PaidDonationCount = snapshot.Summary.PaidDonationCount,
                GeneratedAtUtc = snapshot.GeneratedAtUtc,
            };
        }

        private static string BuildSuccessMessage(int pagesUploaded, int pagesWrittenLocally)
        {
            if (pagesUploaded > 0 && pagesWrittenLocally > 0)
            {
                return $"Published {pagesUploaded} page(s) to blob storage and wrote {pagesWrittenLocally} file(s) locally.";
            }

            if (pagesUploaded > 0)
            {
                return $"Published {pagesUploaded} page(s) to blob storage at {DonationReportPaths.PublicPath}.";
            }

            return $"Wrote {pagesWrittenLocally} report file(s) locally.";
        }

        private static string ResolveBlobContainerName(
            DonationReportOptions options,
            DonationReportGenerationRequest request)
        {
            if (!string.IsNullOrWhiteSpace(options.BlobContainerName))
            {
                return options.BlobContainerName;
            }

            return request?.BlobContainerNameOverride;
        }
    }
}
