// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Web.TenantManagement.Pages
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Index model.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public IndexModel(ILogger<IndexModel> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
        }
    }
}