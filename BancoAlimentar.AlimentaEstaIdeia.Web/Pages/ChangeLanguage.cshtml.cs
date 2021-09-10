// -----------------------------------------------------------------------
// <copyright file="ChangeLanguage.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Change language model.
    /// </summary>
    public class ChangeLanguageModel : PageModel
    {
        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="culture">New culture.</param>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A redirection.</returns>
        public IActionResult OnPost(string culture = null, string returnUrl = null)
        {
            return ChangeLanguage(culture, returnUrl);
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="culture">New culture.</param>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A redirection.</returns>
        public IActionResult OnGet(string culture = null, string returnUrl = null)
        {
            return ChangeLanguage(culture, returnUrl);
        }

        private IActionResult ChangeLanguage(string culture, string returnUrl)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true });
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage("./Index");
            }
        }
    }
}
