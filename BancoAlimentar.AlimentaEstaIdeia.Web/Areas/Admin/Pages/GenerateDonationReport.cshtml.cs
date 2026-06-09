// -----------------------------------------------------------------------
// <copyright file="GenerateDonationReport.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Admin action to generate and publish the static donation analytics report.
    /// </summary>
    public class GenerateDonationReportModel : PageModel
    {
        private readonly IDonationReportGenerationService reportGenerationService;
        private readonly DonationReportGenerationState generationState;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IStringLocalizer<AdminSharedResources> localizer;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateDonationReportModel"/> class.
        /// </summary>
        /// <param name="reportGenerationService">Report generation service.</param>
        /// <param name="generationState">Background generation state.</param>
        /// <param name="serviceScopeFactory">Scope factory for background work.</param>
        /// <param name="webHostEnvironment">Web host environment.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        /// <param name="dbContextFactory">Database context factory.</param>
        public GenerateDonationReportModel(
            IDonationReportGenerationService reportGenerationService,
            DonationReportGenerationState generationState,
            IServiceScopeFactory serviceScopeFactory,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizer<AdminSharedResources> localizer,
            IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            this.reportGenerationService = reportGenerationService;
            this.generationState = generationState;
            this.serviceScopeFactory = serviceScopeFactory;
            this.webHostEnvironment = webHostEnvironment;
            this.localizer = localizer;
            this.dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Gets or sets the success status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the error status message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the last generation result (shown after synchronous run).
        /// </summary>
        public DonationReportGenerationResult LastResult { get; set; }

        /// <summary>
        /// Gets a value indicating whether background generation is running.
        /// </summary>
        public bool IsBackgroundRunning => this.generationState.IsRunning;

        /// <summary>
        /// Gets the SQL Server database name (Initial Catalog) used for report generation.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
            this.LoadDatabaseName();
        }

        /// <summary>
        /// Generates the report and waits for completion.
        /// </summary>
        /// <returns>The current page.</returns>
        public async Task<IActionResult> OnPostGenerateNowAsync()
        {
            if (this.generationState.IsRunning)
            {
                this.ErrorMessage = this.localizer["BackgroundReportInProgress"];
                return this.RedirectToPage();
            }

            try
            {
                this.LastResult = await this.reportGenerationService.GenerateAndPublishAsync(this.BuildRequest());
                if (this.LastResult.Succeeded)
                {
                    this.StatusMessage = this.LastResult.Message;
                }
                else
                {
                    this.ErrorMessage = this.LastResult.Skipped
                        ? this.LastResult.Message
                        : this.LastResult.Message ?? this.localizer["ReportGenerationFailed"];
                }

                this.LoadDatabaseName();
                return this.Page();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = this.localizer["ReportGenerationFailedWithMessage", ex.Message];
                this.LoadDatabaseName();
                return this.Page();
            }
        }

        /// <summary>
        /// Enqueues report generation on a background thread.
        /// </summary>
        /// <returns>Redirect to this page.</returns>
        public IActionResult OnPostEnqueue()
        {
            if (!this.generationState.TryStart())
            {
                this.ErrorMessage = this.localizer["ReportGenerationAlreadyInProgress"];
                return this.RedirectToPage();
            }

            DonationReportGenerationRequest request = this.BuildRequest();
            IServiceScopeFactory scopeFactory = this.serviceScopeFactory;
            DonationReportGenerationState state = this.generationState;

            _ = Task.Run(async () =>
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    IDonationReportGenerationService service =
                        scope.ServiceProvider.GetRequiredService<IDonationReportGenerationService>();
                    await service.GenerateAndPublishAsync(request);
                }
                catch
                {
                    // Background failures are visible in logs; admin can retry from this page.
                }
                finally
                {
                    state.Complete();
                }
            });

            this.StatusMessage = this.localizer["ReportGenerationStartedInBackground"];
            return this.RedirectToPage();
        }

        private DonationReportGenerationRequest BuildRequest()
        {
            DonationReportGenerationRequest request = new DonationReportGenerationRequest
            {
                Force = true,
            };

            Tenant tenant = this.HttpContext.GetTenant();
            if (!string.IsNullOrWhiteSpace(tenant?.NormalizedName))
            {
                request.BlobContainerNameOverride = tenant.NormalizedName;
            }

            // Local disk output is for development only. On Azure App Service the deployed
            // wwwroot folder is not writable; production reports are served from blob storage.
            if (this.webHostEnvironment.IsDevelopment()
                && !string.IsNullOrWhiteSpace(this.webHostEnvironment.WebRootPath))
            {
                request.LocalOutputDirectory = Path.Combine(this.webHostEnvironment.WebRootPath, "report");
            }

            return request;
        }

        private void LoadDatabaseName()
        {
            using ApplicationDbContext context = this.dbContextFactory.CreateDbContext();
            string connectionString = context.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                this.DatabaseName = "(not specified)";
                return;
            }

            var builder = new SqlConnectionStringBuilder(connectionString);
            this.DatabaseName = string.IsNullOrWhiteSpace(builder.InitialCatalog)
                ? "(not specified)"
                : builder.InitialCatalog;
        }
    }
}
