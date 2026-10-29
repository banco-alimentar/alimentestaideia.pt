// -----------------------------------------------------------------------
// <copyright file="Correction.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Creates a separately identified correction from an issued fiscal document.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class CorrectionModel : PageModel
    {
        private readonly IFiscalDocumentCorrectionService fiscalDocumentCorrectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrectionModel"/> class.
        /// </summary>
        /// <param name="fiscalDocumentCorrectionService">The trusted fiscal correction service.</param>
        public CorrectionModel(IFiscalDocumentCorrectionService fiscalDocumentCorrectionService)
        {
            this.fiscalDocumentCorrectionService = fiscalDocumentCorrectionService;
        }

        /// <summary>
        /// Gets or sets the only caller-provided correction values. Tenant, issuer, scope, and actor
        /// are resolved by the correction service.
        /// </summary>
        [BindProperty]
        public FiscalDocumentCorrectionRequest Correction { get; set; } = new FiscalDocumentCorrectionRequest();

        /// <summary>
        /// Gets the correction produced for the authorized request.
        /// </summary>
        public FiscalDocument CorrectionDocument { get; private set; }

        /// <summary>
        /// Creates or returns the idempotent correction and displays its fiscal identifier.
        /// </summary>
        /// <returns>The correction page.</returns>
        public Task<IActionResult> OnPostCorrectAsync()
        {
            if (!this.ModelState.IsValid || this.Correction.OriginalFiscalDocumentId <= 0 ||
                string.IsNullOrWhiteSpace(this.Correction.IdempotencyKey) || string.IsNullOrWhiteSpace(this.Correction.Motive))
            {
                this.ModelState.AddModelError(string.Empty, "Indique o documento original, o identificador único e o motivo da correção.");
                return Task.FromResult<IActionResult>(this.Page());
            }

            try
            {
                this.CorrectionDocument = this.fiscalDocumentCorrectionService.CreateCorrection(this.Correction);
            }
            catch (System.InvalidOperationException)
            {
                this.ModelState.AddModelError(string.Empty, "Não foi possível criar a correção do documento fiscal indicado.");
            }

            return Task.FromResult<IActionResult>(this.Page());
        }
    }
}
