// -----------------------------------------------------------------------
// <copyright file="SiteHealthReport.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Localization;
    using ResolvedSlot = BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth.SiteHealthResolvedSlot;

    /// <summary>
    /// Super-admin dashboard for Application Insights site health.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class SiteHealthReportModel : PageModel
    {
        private readonly ISiteHealthReportService reportService;
        private readonly SiteHealthReportGenerationState generationState;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IStringLocalizer<AdminSharedResources> sharedLocalizer;
        private readonly SiteHealthReportOptions reportOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteHealthReportModel"/> class.
        /// </summary>
        /// <param name="reportService">Site health report service.</param>
        /// <param name="generationState">Generation state tracker.</param>
        /// <param name="serviceScopeFactory">Scope factory for background work.</param>
        /// <param name="sharedLocalizer">Admin shared localizer.</param>
        /// <param name="configuration">Application configuration.</param>
        public SiteHealthReportModel(
            ISiteHealthReportService reportService,
            SiteHealthReportGenerationState generationState,
            IServiceScopeFactory serviceScopeFactory,
            IStringLocalizer<AdminSharedResources> sharedLocalizer,
            IConfiguration configuration)
        {
            this.reportService = reportService;
            this.generationState = generationState;
            this.serviceScopeFactory = serviceScopeFactory;
            this.sharedLocalizer = sharedLocalizer;
            this.reportOptions = SiteHealthReportConfiguration.ReadOptions(configuration);
            this.CurrentSlot = SiteHealthAppRoleResolver.ResolveCurrentSlot(this.reportOptions);
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
        /// Gets or sets the loaded report snapshot.
        /// </summary>
        public SiteHealthReport Report { get; set; }

        /// <summary>
        /// Gets or sets the current generation status.
        /// </summary>
        public SiteHealthReportGenerationStatus GenerationStatus { get; set; }

        /// <summary>
        /// Gets the deployment slot this page is running in.
        /// </summary>
        public ResolvedSlot CurrentSlot { get; }

        /// <summary>
        /// Gets a value indicating whether the loaded report belongs to another slot.
        /// </summary>
        public bool ReportSlotMismatch =>
            this.Report != null &&
            !string.Equals(this.Report.SlotKey, this.CurrentSlot.SlotKey, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the Application Insights cloud role used for investigate links.
        /// </summary>
        public string ActiveAppRoleName => this.CurrentSlot.AppRoleName;

        /// <summary>
        /// Builds an Application Insights / Log Analytics investigate URL for an issue row.
        /// </summary>
        /// <param name="issueCode">Issue code from the report.</param>
        /// <param name="windowLabel">Time window label (24h or 7d).</param>
        /// <returns>Portal URL or null.</returns>
        public string GetInvestigateUrl(string issueCode, string windowLabel)
        {
            return SiteHealthApplicationInsightsLinkBuilder.BuildInvestigateUrlForRole(
                issueCode,
                windowLabel,
                this.ActiveAppRoleName,
                this.reportOptions);
        }

        /// <summary>
        /// Builds an investigate URL for failed HTTP requests in a period.
        /// </summary>
        /// <param name="windowLabel">Time window label.</param>
        /// <returns>Portal URL.</returns>
        public string GetFailedRequestsUrl(string windowLabel)
        {
            return SiteHealthApplicationInsightsLinkBuilder.BuildFailedRequestsUrl(
                windowLabel,
                this.ActiveAppRoleName,
                this.reportOptions);
        }

        /// <summary>
        /// Builds an investigate URL for exceptions in a period.
        /// </summary>
        /// <param name="windowLabel">Time window label.</param>
        /// <returns>Portal URL.</returns>
        public string GetExceptionsUrl(string windowLabel)
        {
            return SiteHealthApplicationInsightsLinkBuilder.BuildExceptionsUrl(
                windowLabel,
                this.ActiveAppRoleName,
                this.reportOptions);
        }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task OnGetAsync()
        {
            await this.LoadAsync();
        }

        /// <summary>
        /// Returns JSON generation status for polling.
        /// </summary>
        /// <returns>Status JSON.</returns>
        public async Task<IActionResult> OnGetStatusAsync()
        {
            SiteHealthReportGenerationStatus status = await this.reportService.GetGenerationStatusAsync();
            return new JsonResult(status);
        }

        /// <summary>
        /// Returns JSON report metadata after refresh.
        /// </summary>
        /// <returns>Report summary JSON.</returns>
        public async Task<IActionResult> OnGetReportAsync()
        {
            SiteHealthReport report = await this.reportService.GetLatestReportAsync();
            if (report == null)
            {
                return new JsonResult(new { hasReport = false });
            }

            return new JsonResult(new
            {
                hasReport = true,
                generatedAtUtc = report.GeneratedAtUtc,
                generatedBy = report.GeneratedBy,
                slotKey = report.SlotKey,
                slotLabel = report.SlotLabel,
            });
        }

        /// <summary>
        /// Enqueues background report refresh.
        /// </summary>
        /// <returns>Redirect to this page.</returns>
        public IActionResult OnPostRefreshBackground()
        {
            if (!this.generationState.TryStart())
            {
                this.ErrorMessage = this.sharedLocalizer["SiteHealthReportAlreadyRunning"];
                return this.RedirectToPage();
            }

            SiteHealthReportGenerationState state = this.generationState;
            IServiceScopeFactory scopeFactory = this.serviceScopeFactory;
            string slotKey = this.CurrentSlot.SlotKey;

            _ = Task.Run(async () =>
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    ISiteHealthReportService service = scope.ServiceProvider.GetRequiredService<ISiteHealthReportService>();
                    await service.GenerateAndStoreAsync("AdminPage", force: true, slotKey: slotKey);
                }
                catch (Exception ex)
                {
                    state.CompleteFailure(ex.Message);
                }
            });

            this.StatusMessage = this.sharedLocalizer["SiteHealthReportRefreshStarted"];
            return this.RedirectToPage();
        }

        private async Task LoadAsync()
        {
            this.Report = await this.reportService.GetLatestReportAsync();
            this.GenerationStatus = await this.reportService.GetGenerationStatusAsync();
        }
    }
}
