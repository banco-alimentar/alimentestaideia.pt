// -----------------------------------------------------------------------
// <copyright file="ResetAuthenticator.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Reset authentication model.
    /// </summary>
    public class ResetAuthenticatorModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly SignInManager<WebUser> signInManager;
        private readonly ILogger<ResetAuthenticatorModel> logger;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetAuthenticatorModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="localizer">Localizer.</param>
        public ResetAuthenticatorModel(
            UserManager<WebUser> userManager,
            SignInManager<WebUser> signInManager,
            ILogger<ResetAuthenticatorModel> logger,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.localizer = localizer;
        }

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

            await userManager.SetTwoFactorEnabledAsync(user, false);
            await userManager.ResetAuthenticatorKeyAsync(user);
            logger.LogInformation("User with ID '{UserId}' has reset their authentication app key.", user.Id);

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = this.localizer["StatusAuthenticatorReset"].Value;

            return RedirectToPage("./EnableAuthenticator");
        }
    }
}