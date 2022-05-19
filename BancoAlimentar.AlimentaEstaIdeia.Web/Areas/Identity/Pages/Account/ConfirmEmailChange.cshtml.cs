// -----------------------------------------------------------------------
// <copyright file="ConfirmEmailChange.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account;

using System.Text;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Confirm email changed model.
/// </summary>
[AllowAnonymous]
public class ConfirmEmailChangeModel : PageModel
{
    private readonly UserManager<WebUser> userManager;
    private readonly SignInManager<WebUser> signInManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmEmailChangeModel"/> class.
    /// </summary>
    /// <param name="userManager">User Manager.</param>
    /// <param name="signInManager">Sign in manager.</param>
    public ConfirmEmailChangeModel(UserManager<WebUser> userManager, SignInManager<WebUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="email">Email.</param>
    /// <param name="code">Email code.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
    {
        if (userId == null || email == null || code == null)
        {
            return RedirectToPage("/Index");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userId}'.");
        }

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ChangeEmailAsync(user, email, code);
        if (!result.Succeeded)
        {
            StatusMessage = "Error changing email.";
            return Page();
        }

        // In our UI email and user name are one and the same, so when we update the email
        // we need to update the user name.
        var setUserNameResult = await userManager.SetUserNameAsync(user, email);
        if (!setUserNameResult.Succeeded)
        {
            StatusMessage = "Error changing user name.";
            return Page();
        }

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Thank you for confirming your email change.";
        return Page();
    }
}
