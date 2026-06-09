// -----------------------------------------------------------------------
// <copyright file="SuperAdmin.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// SuperAdmin landing page with links to operational tools.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class SuperAdminModel : PageModel
    {
        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
