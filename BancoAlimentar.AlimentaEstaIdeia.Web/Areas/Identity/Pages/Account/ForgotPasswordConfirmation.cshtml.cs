// -----------------------------------------------------------------------
// <copyright file="ForgotPasswordConfirmation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Forgot password confirmation model.
/// </summary>
[AllowAnonymous]
public class ForgotPasswordConfirmation : PageModel
{
    /// <summary>
    /// Execute the get operation.
    /// </summary>
    public void OnGet()
    {
    }
}
