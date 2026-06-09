// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Entry page for the RoleManagement area; redirects to user roles.
    /// </summary>
    public class IndexModel : PageModel
    {
        /// <summary>
        /// Redirects to the user roles management page.
        /// </summary>
        /// <returns>Redirect to <see cref="UserRolesModel"/>.</returns>
        public IActionResult OnGet()
        {
            return RedirectToPage("UserRoles");
        }
    }
}
