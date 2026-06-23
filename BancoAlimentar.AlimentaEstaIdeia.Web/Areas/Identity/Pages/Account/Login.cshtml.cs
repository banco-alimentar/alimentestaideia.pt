// -----------------------------------------------------------------------
// <copyright file="Login.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Login model.
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly ApplicationDbContext applicationDbContext;
        private readonly SignInManager<WebUser> signInManager;
        private readonly ILogger<LoginModel> logger;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;
        private readonly IHtmlLocalizer<LoginModel> pageLocalizer;
        private readonly UserLoginTrackingService loginTrackingService;
        private readonly AccountMergeService accountMergeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginModel"/> class.
        /// </summary>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="applicationDbContext">EF Core context.</param>
        /// <param name="localizer">Localizer.</param>
        /// <param name="pageLocalizer">Login page localizer.</param>
        /// <param name="loginTrackingService">Login tracking service.</param>
        /// <param name="accountMergeService">Account merge service.</param>
        public LoginModel(
            SignInManager<WebUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<WebUser> userManager,
            ApplicationDbContext applicationDbContext,
            IHtmlLocalizer<IdentitySharedResources> localizer,
            IHtmlLocalizer<LoginModel> pageLocalizer,
            UserLoginTrackingService loginTrackingService,
            AccountMergeService accountMergeService)
        {
            this.userManager = userManager;
            this.applicationDbContext = applicationDbContext;
            this.localizer = localizer;
            this.pageLocalizer = pageLocalizer;
            this.signInManager = signInManager;
            this.logger = logger;
            this.loginTrackingService = loginTrackingService;
            this.accountMergeService = accountMergeService;
        }

        /// <summary>
        /// Gets or sets the input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets the list of external logins.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Gets or sets the return url.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is signing in to link an external provider.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool LinkExternalLogin { get; set; }

        /// <summary>
        /// Gets or sets the display name of the external provider pending link.
        /// </summary>
        public string PendingExternalProviderDisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to offer a secure account merge.
        /// </summary>
        public bool ShowMergeOffer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show link conflict guidance.
        /// </summary>
        public bool ShowLinkConflictHelp { get; set; }

        /// <summary>
        /// Gets or sets the blocked merge reason key for localization.
        /// </summary>
        public string LinkConflictBlockReason { get; set; }

        /// <summary>
        /// Gets or sets the masked email of the account that already owns the provider.
        /// </summary>
        public string MaskedSourceEmail { get; set; }

        /// <summary>
        /// Gets or sets the masked email returned by the external provider.
        /// </summary>
        public string MaskedExternalEmail { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <param name="donate">Donate.</param>
        /// <param name="error">Error message from external authentication.</param>
        /// <param name="linkExternalLogin">When true, preserve the external login cookie to link after sign-in.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync(string returnUrl = null, bool donate = false, string error = null, bool linkExternalLogin = false)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            this.LinkExternalLogin = linkExternalLogin;

            if (linkExternalLogin)
            {
                var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();
                if (externalLoginInfo != null)
                {
                    this.PendingExternalProviderDisplayName = externalLoginInfo.ProviderDisplayName;
                    this.Input ??= new InputModel();
                    this.Input.Email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email)
                        ?? this.Input.Email;
                }
                else
                {
                    ModelState.AddModelError(
                        string.Empty,
                        this.pageLocalizer["LinkExternalLoginSessionExpired"].Value);
                }
            }
            else
            {
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            }

            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (donate)
            {
                ReturnUrl = Url.Content("~/Donation");
            }
            else
            {
                ReturnUrl = returnUrl;
            }
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(Input.Email);

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await signInManager.PasswordSignInAsync(
                    user != null ? user.UserName : Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    logger.LogInformation("User logged in.");
                    var signedInUser = user ?? await userManager.FindByEmailAsync(Input.Email);
                    if (signedInUser != null)
                    {
                        await this.loginTrackingService.RecordLoginAsync(signedInUser, UserLoginProviders.Password);
                    }

                    var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();
                    if (this.LinkExternalLogin && externalLoginInfo == null)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            this.pageLocalizer["LinkExternalLoginSessionExpired"].Value);
                        return Page();
                    }

                    if (externalLoginInfo != null && signedInUser != null)
                    {
                        return await this.CompleteExternalLoginLinkAsync(
                            signedInUser,
                            externalLoginInfo,
                            returnUrl);
                    }

                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    WebUser loginUser = await userManager.FindByEmailAsync(Input.Email);
                    if (loginUser != null)
                    {
                        var all = this.applicationDbContext.UserLogins.ToList();

                        ApplicationUserLogin externalLogin = this.applicationDbContext.UserLogins
                            .Where(p => p.User.Id == loginUser.Id)
                            .FirstOrDefault();
                        if (externalLogin != null && loginUser.PasswordHash == null)
                        {
                            ModelState.AddModelError(string.Empty, $"You can't login using a password. This account was created using the {externalLogin.ProviderDisplayName} identity provider. Please sign-in using {externalLogin.ProviderDisplayName}.");
                        }
                    }

                    ModelState.AddModelError(string.Empty, this.localizer["InvalidLoginAttempt"].Value);
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Merges the duplicate account into the signed-in account after provider verification.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostConfirmMergeAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            var signedInUser = await userManager.GetUserAsync(User);
            if (signedInUser == null)
            {
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();
            if (externalLoginInfo == null)
            {
                ModelState.AddModelError(string.Empty, this.pageLocalizer["LinkExternalLoginSessionExpired"].Value);
                this.LinkExternalLogin = true;
                return Page();
            }

            return await this.CompleteExternalLoginLinkAsync(
                signedInUser,
                externalLoginInfo,
                returnUrl);
        }

        /// <summary>
        /// Cancels a pending external login link or merge attempt.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostCancelLinkAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            this.LinkExternalLogin = false;
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        private async Task<IActionResult> CompleteExternalLoginLinkAsync(
            WebUser signedInUser,
            ExternalLoginInfo externalLoginInfo,
            string returnUrl)
        {
            var linkAttempt = await accountMergeService.TryLinkExternalLoginAsync(signedInUser, externalLoginInfo);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            if (linkAttempt.Succeeded)
            {
                logger.LogInformation(
                    linkAttempt.Status == ExternalLoginLinkStatus.Merged
                        ? "User merged accounts and linked {Provider}."
                        : "User linked {Provider} to an existing account.",
                    externalLoginInfo.LoginProvider);
                await signInManager.RefreshSignInAsync(signedInUser);
                return LocalRedirect(returnUrl);
            }

            return await this.ShowExternalLoginLinkConflictAsync(
                signedInUser,
                externalLoginInfo,
                linkAttempt);
        }

        private async Task<IActionResult> ShowExternalLoginLinkConflictAsync(
            WebUser signedInUser,
            ExternalLoginInfo externalLoginInfo,
            ExternalLoginLinkAttempt linkAttempt = null)
        {
            var eligibility = linkAttempt?.Conflict
                ?? await accountMergeService.EvaluateMergeAsync(signedInUser, externalLoginInfo);

            this.LinkExternalLogin = true;
            this.PendingExternalProviderDisplayName = externalLoginInfo.ProviderDisplayName;

            if (eligibility.CanMerge)
            {
                this.ShowMergeOffer = true;
                this.MaskedSourceEmail = eligibility.MaskedSourceEmail;
                return Page();
            }

            this.ShowLinkConflictHelp = true;
            this.LinkConflictBlockReason = eligibility.BlockReason.ToString();
            this.MaskedSourceEmail = eligibility.MaskedSourceEmail;
            this.MaskedExternalEmail = eligibility.MaskedExternalEmail;

            if (linkAttempt?.Error != null)
            {
                foreach (var error in linkAttempt.Error.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    this.pageLocalizer["LinkExternalLoginFailed", externalLoginInfo.ProviderDisplayName].Value);
            }

            return Page();
        }

        /// <summary>
        /// Input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets the email address.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the password.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether if remember me or not.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }
    }
}
