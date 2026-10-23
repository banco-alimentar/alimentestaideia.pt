// -----------------------------------------------------------------------
// <copyright file="SaftExport.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Exports SAF-T for an issuer and period within the current tenant.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class SaftExportModel : PageModel
    {
        private readonly IFiscalAdministrationService fiscalAdministrationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaftExportModel"/> class.
        /// </summary>
        /// <param name="fiscalAdministrationService">The trusted fiscal administration service.</param>
        public SaftExportModel(IFiscalAdministrationService fiscalAdministrationService)
        {
            this.fiscalAdministrationService = fiscalAdministrationService;
        }

        /// <summary>
        /// Gets or sets the fiscal issuer profile selected for export.
        /// </summary>
        [BindProperty]
        public int FiscalIssuerProfileId { get; set; }

        /// <summary>
        /// Gets or sets the inclusive export period start date.
        /// </summary>
        [BindProperty]
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Gets or sets the inclusive export period end date.
        /// </summary>
        [BindProperty]
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Creates the requested SAF-T export for the current tenant.
        /// </summary>
        /// <returns>The generated SAF-T XML file.</returns>
        public Task<IActionResult> OnPostExportAsync()
        {
            if (!this.ModelState.IsValid || this.FiscalIssuerProfileId <= 0 || this.StartDate == default ||
                this.EndDate == default || this.EndDate < this.StartDate)
            {
                this.ModelState.AddModelError(string.Empty, "Indique um emitente e um intervalo de datas válido.");
                return Task.FromResult<IActionResult>(this.Page());
            }

            try
            {
                string xml = this.fiscalAdministrationService.ExportSaft(
                    this.FiscalIssuerProfileId,
                    this.StartDate,
                    this.EndDate);

                return Task.FromResult<IActionResult>(this.File(Encoding.UTF8.GetBytes(xml), "application/xml", "saft-pt.xml"));
            }
            catch (System.InvalidOperationException)
            {
                this.ModelState.AddModelError(string.Empty, "Não foi possível exportar o ficheiro SAF-T. Confirme os dados introduzidos.");
                return Task.FromResult<IActionResult>(this.Page());
            }
        }
    }
}
