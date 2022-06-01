// -----------------------------------------------------------------------
// <copyright file="ForgotPasswordConfirmation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Forgot password confirmation model.
    /// </summary>
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        /// <summary>
        /// Gets a value indicating whether the page containing the email not confirmed note is shown.
        /// </summary>
        public bool DisplayEmailNotConfirmedNote { get; private set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Execute the get operation with email not confirmed note.
        /// </summary>
        public void OnGetWithEmailNotConfirmedNote()
        {
          DisplayEmailNotConfirmedNote = true;
        }
  }
}
