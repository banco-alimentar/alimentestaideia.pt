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
        public FiscalDocumentCancellationRequest Cancellation { get; set; }

        /// <summary>
        /// Cancels the selected issued fiscal document while retaining its fiscal evidence.
        /// </summary>
        /// <returns>The fiscal document list.</returns>
        public Task<IActionResult> OnPostCancelAsync()
        {
            this.fiscalDocumentCancellationService.Cancel(this.Cancellation);
            return Task.FromResult<IActionResult>(this.RedirectToPage("./Documents"));
        }
    }
}
