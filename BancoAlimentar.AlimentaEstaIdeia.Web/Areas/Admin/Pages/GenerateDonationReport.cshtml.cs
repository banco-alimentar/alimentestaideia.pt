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
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Admin action to generate and publish the static donation analytics report.
    /// </summary>
    public class GenerateDonationReportModel : PageModel
    {
        private readonly IDonationReportGenerationService reportGenerationService;
        private readonly DonationReportGenerationState generationState;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateDonationReportModel"/> class.
        /// </summary>
        /// <param name="reportGenerationService">Report generation service.</param>
        /// <param name="generationState">Background generation state.</param>
        /// <param name="serviceScopeFactory">Scope factory for background work.</param>
        /// <param name="webHostEnvironment">Web host environment.</param>
        public GenerateDonationReportModel(
            IDonationReportGenerationService reportGenerationService,
            DonationReportGenerationState generationState,
            IServiceScopeFactory serviceScopeFactory,
            IWebHostEnvironment webHostEnvironment)
        {
            this.reportGenerationService = reportGenerationService;
            this.generationState = generationState;
            this.serviceScopeFactory = serviceScopeFactory;
            this.webHostEnvironment = webHostEnvironment;
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
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Generates the report and waits for completion.
        /// </summary>
        /// <returns>The current page.</returns>
        public async Task<IActionResult> OnPostGenerateNowAsync()
        {
            if (this.generationState.IsRunning)
            {
                this.ErrorMessage = "A background report generation is already in progress.";
                return this.RedirectToPage();
            }

            try
            {
                this.LastResult = await this.reportGenerationService.GenerateAndPublishAsync(this.BuildRequest());
                if (this.LastResult.Succeeded)
                {
                    this.StatusMessage = this.LastResult.Message;
                }
                else if (this.LastResult.Skipped)
                {
                    this.ErrorMessage = this.LastResult.Message;
                }
                else
                {
                    this.ErrorMessage = this.LastResult.Message ?? "Report generation failed.";
                }

                return this.Page();
            }
            catch (Exception ex)
            {
                this.ErrorMessage = "Report generation failed: " + ex.Message;
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
                this.ErrorMessage = "A report generation is already in progress.";
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

            this.StatusMessage = "Report generation started in the background. Refresh /report/ in a few minutes.";
            return this.RedirectToPage();
        }

        private DonationReportGenerationRequest BuildRequest()
        {
            DonationReportGenerationRequest request = new DonationReportGenerationRequest
            {
                Force = true,
            };

            if (this.webHostEnvironment.IsDevelopment())
            {
                request.LocalOutputDirectory = Path.Combine(this.webHostEnvironment.WebRootPath, "report");
            }

            return request;
        }
    }
}
