// -----------------------------------------------------------------------
// <copyright file="Referral.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// The referal redirection page.
    /// </summary>
    public class ReferralModel : PageModel
    {
        /// <summary>
        /// Add cookie and redirect to donations.
        /// </summary>
        /// <param name="text">The referal code.</param>
        /// <returns>Redirect to the donations page.</returns>
        public ActionResult OnGet(string text)
        {
            this.Response.Cookies.Append(
                "Referral",
                text,
                new CookieOptions()
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Expires = DateTimeOffset.Now.AddMonths(1),
                });
            return this.RedirectToPage("./Donation", new { referral = text });
        }
    }
}
