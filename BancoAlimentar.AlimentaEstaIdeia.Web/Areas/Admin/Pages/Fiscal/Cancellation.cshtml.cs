// -----------------------------------------------------------------------
// <copyright file="Cancellation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Cancels an issued fiscal document through the trusted tenant-scoped service.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class CancellationModel : PageModel
    {
        private readonly IFiscalDocumentCancellationService fiscalDocumentCancellationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancellationModel"/> class.
        /// </summary>
        /// <param name="fiscalDocumentCancellationService">The trusted fiscal cancellation service.</param>
        public CancellationModel(IFiscalDocumentCancellationService fiscalDocumentCancellationService)
        {
            this.fiscalDocumentCancellationService = fiscalDocumentCancellationService;
        }

        /// <summary>
        /// Gets or sets the only caller-provided cancellation values. Tenant, issuer, scope, and actor
        /// are resolved by the cancellation service.
        /// </summary>
        [BindProperty]
        public FiscalDocumentCancellationRequest Cancellation { get; set; } = new FiscalDocumentCancellationRequest();

        /// <summary>
        /// Cancels the selected issued fiscal document while retaining its fiscal evidence.
        /// </summary>
        /// <returns>The fiscal document list.</returns>
        public Task<IActionResult> OnPostCancelAsync()
        {
            if (!this.ModelState.IsValid || this.Cancellation.FiscalDocumentId <= 0 || string.IsNullOrWhiteSpace(this.Cancellation.Motive))
            {
                this.ModelState.AddModelError(string.Empty, "Indique o documento fiscal e o motivo da anulação.");
                return Task.FromResult<IActionResult>(this.Page());
            }

            try
            {
                this.fiscalDocumentCancellationService.Cancel(this.Cancellation);
            }
            catch (System.InvalidOperationException)
            {
                this.ModelState.AddModelError(string.Empty, "Não foi possível anular o documento fiscal indicado.");
                return Task.FromResult<IActionResult>(this.Page());
            }

            return Task.FromResult<IActionResult>(this.RedirectToPage("./Documents"));
        }
    }
}
