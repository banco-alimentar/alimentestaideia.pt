// -----------------------------------------------------------------------
// <copyright file="SuperAdmin.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Legacy route that redirects to the admin home page.
    /// </summary>
    public class SuperAdminModel : PageModel
    {
        /// <summary>
        /// Redirect to the admin home page.
        /// </summary>
        /// <returns>Redirect to admin index.</returns>
        public IActionResult OnGet()
        {
            return this.RedirectToPage("/Index");
        }
    }
}
