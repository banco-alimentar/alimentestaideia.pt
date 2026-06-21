// -----------------------------------------------------------------------
// <copyright file="ExternalLogins.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Extenal login model.
    /// </summary>
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly SignInManager<WebUser> signInManager;
        private readonly AccountMergeService accountMergeService;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalLoginsModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="accountMergeService">Account merge service.</param>
        /// <param name="localizer">Localizer.</param>
        public ExternalLoginsModel(
            UserManager<WebUser> userManager,
            SignInManager<WebUser> signInManager,
            AccountMergeService accountMergeService,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.accountMergeService = accountMergeService;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the list of <see cref="UserLoginInfo"/>.
        /// </summary>
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="AuthenticationScheme"/>.
        /// </summary>
        public IList<AuthenticationScheme> OtherLogins { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the remove button or not.
        /// </summary>
        public bool ShowRemoveButton { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to offer a secure account merge.
        /// </summary>
        [TempData]
        public bool ShowMergeOffer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show link conflict guidance.
        /// </summary>
        [TempData]
        public bool ShowLinkConflictHelp { get; set; }

        /// <summary>
        /// Gets or sets the blocked merge reason key for localization.
        /// </summary>
        [TempData]
        public string LinkConflictBlockReason { get; set; }

        /// <summary>
        /// Gets or sets the provider display name involved in the conflict.
        /// </summary>
        [TempData]
        public string MergeProviderDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the masked email of the account that already owns the provider.
        /// </summary>
        [TempData]
        public string MaskedSourceEmail { get; set; }

        /// <summary>
        /// Gets or sets the masked email returned by the external provider.
        /// </summary>
        [TempData]
        public string MaskedExternalEmail { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            CurrentLogins = await userManager.GetLoginsAsync(user);
            OtherLogins = (await signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            ShowRemoveButton = user.PasswordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        /// <summary>
        /// Execute the remove login post operation.
        /// </summary>
        /// <param name="loginProvider">Login provider name.</param>
        /// <param name="providerKey">Provider key.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var result = await userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = this.localizer["StatusExternalLoginNotRemoved"].Value;
                return RedirectToPage();
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = this.localizer["StatusExternalLoginRemoved"].Value;
            return RedirectToPage();
        }

        /// <summary>
        /// Execute the link to login operation.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = signInManager.ConfigureExternalAuthenticationProperties(
                provider,
                redirectUrl,
                userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Executed the link login call back operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                throw new InvalidOperationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            var linkAttempt = await this.accountMergeService.TryLinkExternalLoginAsync(user, info);
            if (linkAttempt.Succeeded)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                StatusMessage = linkAttempt.Status == ExternalLoginLinkStatus.Merged
                    ? this.localizer["StatusExternalLoginMerged"].Value
                    : this.localizer["StatusExternalLoginAdded"].Value;
                await signInManager.RefreshSignInAsync(user);
                return RedirectToPage();
            }

            return await this.HandleLinkConflictAsync(user, info, linkAttempt);
        }

        /// <summary>
        /// Merges the duplicate account into the signed-in account after provider verification.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostConfirmMergeAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID 'user.Id'.");
            }

            var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                StatusMessage = this.localizer["StatusExternalLoginSessionExpired"].Value;
                return RedirectToPage();
            }

            var linkAttempt = await this.accountMergeService.TryLinkExternalLoginAsync(user, info);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (!linkAttempt.Succeeded)
            {
                return await this.HandleLinkConflictAsync(user, info, linkAttempt);
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = linkAttempt.Status == ExternalLoginLinkStatus.Merged
                ? this.localizer["StatusExternalLoginMerged"].Value
                : this.localizer["StatusExternalLoginAdded"].Value;
            return RedirectToPage();
        }

        /// <summary>
        /// Cancels a pending external login link or merge attempt.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostCancelLinkAsync()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return RedirectToPage();
        }

        private async Task<IActionResult> HandleLinkConflictAsync(
            WebUser user,
            ExternalLoginInfo info,
            ExternalLoginLinkAttempt linkAttempt = null)
        {
            var eligibility = linkAttempt?.Conflict
                ?? await accountMergeService.EvaluateMergeAsync(user, info);
            MergeProviderDisplayName = info.ProviderDisplayName;

            if (eligibility.CanMerge)
            {
                ShowMergeOffer = true;
                MaskedSourceEmail = eligibility.MaskedSourceEmail;
                return RedirectToPage();
            }

            ShowLinkConflictHelp = true;
            LinkConflictBlockReason = eligibility.BlockReason.ToString();
            MaskedSourceEmail = eligibility.MaskedSourceEmail;
            MaskedExternalEmail = eligibility.MaskedExternalEmail;
            return RedirectToPage();
        }
    }
}
