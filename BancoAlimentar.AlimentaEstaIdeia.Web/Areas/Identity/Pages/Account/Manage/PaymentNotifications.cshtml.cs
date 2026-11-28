// -----------------------------------------------------------------------
// <copyright file="PaymentNotifications.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Redirects the former payment notification page to the combined communications page.
    /// </summary>
    public class PaymentNotificationsModel : PageModel
    {
        /// <summary>
        /// Executes the get operation.
        /// </summary>
        /// <returns>A redirect to the combined communications page.</returns>
        public IActionResult OnGet()
        {
            return this.RedirectToPage("./Communications");
        }
    }
}
