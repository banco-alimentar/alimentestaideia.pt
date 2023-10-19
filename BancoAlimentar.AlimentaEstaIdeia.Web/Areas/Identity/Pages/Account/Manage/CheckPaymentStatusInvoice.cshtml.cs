// -----------------------------------------------------------------------
// <copyright file="CheckPaymentStatusInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Page for waiting for the payment to be confirmed.
    /// </summary>
    public class CheckPaymentStatusInvoiceModel : PageModel
    {
        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
            // check confirmed payment id.
        }
    }
}
