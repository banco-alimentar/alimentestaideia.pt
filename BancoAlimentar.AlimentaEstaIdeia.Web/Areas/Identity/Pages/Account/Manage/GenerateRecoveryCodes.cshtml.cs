// -----------------------------------------------------------------------
// <copyright file="GenerateRecoveryCodes.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;

using System;
using System.Linq;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Generate recovery codes model.
/// </summary>
public class GenerateRecoveryCodesModel : PageModel
{
    private readonly UserManager<WebUser> userManager;
    private readonly ILogger<GenerateRecoveryCodesModel> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateRecoveryCodesModel"/> class.
    /// </summary>
    /// <param name="userManager">User Manager.</param>
    /// <param name="logger">Logger.</param>
    public GenerateRecoveryCodesModel(
        UserManager<WebUser> userManager,
        ILogger<GenerateRecoveryCodesModel> logger)
    {
        this.userManager = userManager;
        this.logger = logger;
    }

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
    /// Execute the get operation.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
        {
            var userId = await userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' because they do not have 2FA enabled.");
        }

        return Page();
    }

    /// <summary>
    /// Execute the post operation.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        var userId = await userManager.GetUserIdAsync(user);
        if (!isTwoFactorEnabled)
        {
            throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
        }

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = recoveryCodes.ToArray();

        logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
        StatusMessage = "You have generated new recovery codes.";
        return RedirectToPage("./ShowRecoveryCodes");
    }
}
