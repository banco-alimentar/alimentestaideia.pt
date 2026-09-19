// -----------------------------------------------------------------------
// <copyright file="Series.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Configures and closes communicated fiscal document series for the current tenant.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class SeriesModel : PageModel
    {
        private readonly IFiscalAdministrationService fiscalAdministrationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesModel"/> class.
        /// </summary>
        /// <param name="fiscalAdministrationService">The trusted fiscal administration service.</param>
        public SeriesModel(IFiscalAdministrationService fiscalAdministrationService)
        {
            this.fiscalAdministrationService = fiscalAdministrationService;
        }

        /// <summary>
        /// Gets or sets the communicated, non-secret fiscal series configuration.
        /// </summary>
        [BindProperty]
        public FiscalDocumentSeriesAdministrationRequest Series { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the series to close.
        /// </summary>
        [BindProperty]
        public int FiscalDocumentSeriesId { get; set; }

        /// <summary>
        /// Configures a fiscal series using its AT communication evidence.
        /// </summary>
        /// <returns>The fiscal series page.</returns>
        public Task<IActionResult> OnPostConfigureAsync()
        {
            this.fiscalAdministrationService.ConfigureSeries(this.Series);
            return Task.FromResult<IActionResult>(this.RedirectToPage());
        }

        /// <summary>
        /// Closes the specified fiscal series without deleting it.
        /// </summary>
        /// <returns>The fiscal series page.</returns>
        public Task<IActionResult> OnPostCloseAsync()
        {
            this.fiscalAdministrationService.CloseSeries(this.FiscalDocumentSeriesId);
            return Task.FromResult<IActionResult>(this.RedirectToPage());
        }
    }
}
