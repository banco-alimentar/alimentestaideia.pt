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
        public LoginModel(
            SignInManager<WebUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<WebUser> userManager,
            ApplicationDbContext applicationDbContext,
            IHtmlLocalizer<IdentitySharedResources> localizer,
            IHtmlLocalizer<LoginModel> pageLocalizer,
            UserLoginTrackingService loginTrackingService)
        {
            this.userManager = userManager;
            this.applicationDbContext = applicationDbContext;
            this.localizer = localizer;
            this.pageLocalizer = pageLocalizer;
            this.signInManager = signInManager;
            this.logger = logger;
            this.loginTrackingService = loginTrackingService;
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
        public bool LinkExternalLogin { get; set; }

        /// <summary>
        /// Gets or sets the display name of the external provider pending link.
        /// </summary>
        public string PendingExternalProviderDisplayName { get; set; }

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
                    if (externalLoginInfo != null && signedInUser != null)
                    {
                        var linkResult = await userManager.AddLoginAsync(signedInUser, externalLoginInfo);
                        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                        if (linkResult.Succeeded)
                        {
                            logger.LogInformation(
                                "User linked {Provider} to an existing account.",
                                externalLoginInfo.LoginProvider);
                            await signInManager.RefreshSignInAsync(signedInUser);
                            return LocalRedirect(returnUrl);
                        }

                        ModelState.AddModelError(
                            string.Empty,
                            string.Format(
                                this.pageLocalizer["LinkExternalLoginFailed"].Value,
                                externalLoginInfo.ProviderDisplayName));
                        this.LinkExternalLogin = true;
                        this.PendingExternalProviderDisplayName = externalLoginInfo.ProviderDisplayName;
                        return Page();
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
