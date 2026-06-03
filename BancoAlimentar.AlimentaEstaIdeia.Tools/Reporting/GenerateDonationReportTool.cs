// -----------------------------------------------------------------------
// <copyright file="GenerateDonationReportTool.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Reporting
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Generates donation report HTML locally from the configured database.
    /// </summary>
    public static class GenerateDonationReportTool
    {
        /// <summary>
        /// Default output folder relative to the Tools project (Web wwwroot/report).
        /// </summary>
        public static readonly string DefaultOutputDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BancoAlimentar.AlimentaEstaIdeia.Web", "wwwroot", "report"));

        /// <summary>
        /// Builds report HTML and writes files to disk.
        /// </summary>
        /// <param name="configuration">Application configuration with database connection.</param>
        /// <param name="unitOfWork">Unit of work.</param>
        /// <param name="applicationDbContext">Database context.</param>
        /// <param name="outputDirectory">Target directory for HTML files.</param>
        /// <returns>A task.</returns>
        public static async Task ExecuteAsync(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ApplicationDbContext applicationDbContext,
            string outputDirectory)
        {
            DonationReportGenerationService service = new DonationReportGenerationService();
            DonationReportGenerationResult result = await service.GenerateAndPublishAsync(
                configuration,
                unitOfWork,
                applicationDbContext,
                new DonationReportGenerationRequest
                {
                    Force = true,
                    LocalOutputDirectory = outputDirectory,
                });

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Message ?? "Donation report generation failed.");
            }

            Console.WriteLine(result.Message);
            Console.WriteLine($"Output directory: {outputDirectory}");
            Console.WriteLine($"Open in browser: {DonationReportPaths.PublicPath} (after starting the Web app)");
            Console.WriteLine($"Total paid: {result.TotalPaidAmount:C2} ({result.PaidDonationCount} donations)");
        }
    }
}
