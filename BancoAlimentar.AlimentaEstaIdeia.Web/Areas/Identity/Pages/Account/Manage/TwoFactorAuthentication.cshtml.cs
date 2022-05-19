// -----------------------------------------------------------------------
// <copyright file="TwoFactorAuthentication.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;

using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Two factor authentication model.
/// </summary>
public class TwoFactorAuthenticationModel : PageModel
{
    private const string AuthenicatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";

    private readonly UserManager<WebUser> userManager;
    private readonly SignInManager<WebUser> signInManager;
    private readonly ILogger<TwoFactorAuthenticationModel> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorAuthenticationModel"/> class.
    /// </summary>
    /// <param name="userManager">User Manager.</param>
    /// <param name="signInManager">Sign in manager.</param>
    /// <param name="logger">Logger.</param>
    public TwoFactorAuthenticationModel(
        UserManager<WebUser> userManager,
        SignInManager<WebUser> signInManager,
        ILogger<TwoFactorAuthenticationModel> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    /// <summary>
    /// Gets or sets a value indicating whether there is an authenticatior setup.
    /// </summary>
    public bool HasAuthenticator { get; set; }

    /// <summary>
    /// Gets or sets the recovery codes left.
    /// </summary>
    public int RecoveryCodesLeft { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if second factor of authentication is enabled.
    /// </summary>
    [BindProperty]
    public bool Is2faEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if machine is remembered.
    /// </summary>
    public bool IsMachineRemembered { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnGet()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        HasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user) != null;
        Is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user);
        RecoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);

        return Page();
    }

    /// <summary>
    /// Execute the post operation.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnPost()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await signInManager.ForgetTwoFactorClientAsync();
        StatusMessage = "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.";
        return RedirectToPage();
    }
}
