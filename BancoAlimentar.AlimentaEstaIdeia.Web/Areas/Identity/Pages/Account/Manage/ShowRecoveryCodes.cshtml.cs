// -----------------------------------------------------------------------
// <copyright file="ShowRecoveryCodes.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Show recovery codes model.
/// </summary>
public class ShowRecoveryCodesModel : PageModel
{
    /// <summary>
    /// Gets or sets the recovery codes.
    /// </summary>
    [TempData]
    public string[] RecoveryCodes { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    /// <summary>
    /// Executes the get operation.
    /// </summary>
    /// <returns>Page.</returns>
    public IActionResult OnGet()
    {
        if (RecoveryCodes == null || RecoveryCodes.Length == 0)
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        return Page();
    }
}
