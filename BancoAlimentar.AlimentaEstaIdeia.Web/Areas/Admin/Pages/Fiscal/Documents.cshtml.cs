// -----------------------------------------------------------------------
// <copyright file="Documents.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Lists safe fiscal-document summaries through the trusted tenant-scoped query service.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class DocumentsModel : PageModel
    {
        private readonly IFiscalDocumentQueryService fiscalDocumentQueryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsModel"/> class.
        /// </summary>
        /// <param name="fiscalDocumentQueryService">The trusted fiscal document query service.</param>
        public DocumentsModel(IFiscalDocumentQueryService fiscalDocumentQueryService)
        {
            this.fiscalDocumentQueryService = fiscalDocumentQueryService;
        }

        /// <summary>
        /// Gets or sets the user-selectable document query filters. It has no tenant or food-bank boundary.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public FiscalDocumentQueryRequest Query { get; set; }

        /// <summary>
        /// Gets the safe fiscal document summaries matching the selected filters.
        /// </summary>
        public IReadOnlyList<FiscalDocumentQueryResult> Documents { get; private set; }

        /// <summary>
        /// Queries fiscal document summaries for the selected issuer and filters.
        /// </summary>
        /// <returns>A task representing the read operation.</returns>
        public Task OnGetAsync()
        {
            this.Documents = this.fiscalDocumentQueryService.Query(this.Query);
            return Task.CompletedTask;
        }
    }
}
