// -----------------------------------------------------------------------
// <copyright file="Login.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Login model.
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private const string EmailCodeSessionKey = "LoginEmailCode";
        private const int EmailCodeMinimum = 100000;
        private const int EmailCodeMaximum = 1000000;
        private const int EmailCodeLifetimeMinutes = 10;
        private const int EmailCodeMaximumAttempts = 5;
        private const int EmailCodeResendCooldownSeconds = 60;

        private readonly UserManager<WebUser> userManager;
        private readonly ApplicationDbContext applicationDbContext;
        private readonly SignInManager<WebUser> signInManager;
        private readonly ILogger<LoginModel> logger;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender;
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
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="localizer">Localizer.</param>
        /// <param name="pageLocalizer">Login page localizer.</param>
        /// <param name="loginTrackingService">Login tracking service.</param>
        /// <param name="accountMergeService">Account merge service.</param>
        public LoginModel(
            SignInManager<WebUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<WebUser> userManager,
            ApplicationDbContext applicationDbContext,
            Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender,
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
            this.emailSender = emailSender;
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
        /// Gets or sets the email-code input model.
        /// </summary>
        [ValidateNever]
        [BindProperty]
        public EmailCodeInputModel EmailCodeInput { get; set; }

        /// <summary>
        /// Gets or sets the one-time email login code.
        /// </summary>
        [ValidateNever]
        [BindProperty]
        public string EmailLoginCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email-code form is displayed.
        /// </summary>
        public bool ShowEmailCodeForm { get; set; }

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

            this.EmailCodeInput ??= new EmailCodeInputModel();
            if (this.LinkExternalLogin && string.IsNullOrWhiteSpace(this.EmailCodeInput.Email))
            {
                this.EmailCodeInput.Email = this.Input?.Email;
            }
        }

        /// <summary>
        /// Sends a one-time code for passwordless login.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostRequestEmailCodeAsync(string returnUrl = null)
        {
            return await this.StartEmailCodeAsync(returnUrl);
        }

        /// <summary>
        /// Resends the one-time code for passwordless login.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostResendEmailCodeAsync(string returnUrl = null)
        {
            return await this.StartEmailCodeAsync(returnUrl);
        }

        /// <summary>
        /// Verifies the one-time code and signs the user in.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostVerifyEmailCodeAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            await this.LoadLoginPageDataAsync(returnUrl);
            ShowEmailCodeForm = true;

            var state = HttpContext.Session.GetObjectFromJson<EmailCodeState>(EmailCodeSessionKey);
            if (state == null)
            {
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeExpired"].Value);
                return Page();
            }

            EmailCodeInput ??= new EmailCodeInputModel();
            EmailCodeInput.Email = state.Email;
            var user = await userManager.FindByEmailAsync(state.Email);
            if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
            {
                HttpContext.Session.Remove(EmailCodeSessionKey);
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeInvalid"].Value);
                return Page();
            }

            if (state.ExpiresAtUtc < DateTime.UtcNow)
            {
                HttpContext.Session.Remove(EmailCodeSessionKey);
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeExpired"].Value);
                return Page();
            }

            if (state.Attempts >= EmailCodeMaximumAttempts)
            {
                HttpContext.Session.Remove(EmailCodeSessionKey);
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeTooManyAttempts"].Value);
                return Page();
            }

            state.Attempts++;
            string enteredCode = EmailLoginCode?.Trim() ?? string.Empty;
            if (!this.IsEmailCodeValid(state, enteredCode))
            {
                if (state.Attempts >= EmailCodeMaximumAttempts)
                {
                    HttpContext.Session.Remove(EmailCodeSessionKey);
                    ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeTooManyAttempts"].Value);
                }
                else
                {
                    HttpContext.Session.SaveObjectAsJson(EmailCodeSessionKey, state);
                    ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeInvalid"].Value);
                }

                return Page();
            }

            HttpContext.Session.Remove(EmailCodeSessionKey);
            if (!await signInManager.CanSignInAsync(user))
            {
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeInvalid"].Value);
                return Page();
            }

            if (await signInManager.IsTwoFactorEnabledAsync(user)
                && !await signInManager.IsTwoFactorClientRememberedAsync(user))
            {
                var twoFactorIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
                twoFactorIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, await userManager.GetUserIdAsync(user)));
                twoFactorIdentity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, UserLoginProviders.EmailCode));
                var twoFactorPrincipal = new ClaimsPrincipal(twoFactorIdentity);
                await HttpContext.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, twoFactorPrincipal);
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = state.RememberMe });
            }

            await signInManager.SignInAsync(user, state.RememberMe, UserLoginProviders.EmailCode);

            logger.LogInformation("User logged in with an email verification code.");
            await this.loginTrackingService.RecordLoginAsync(user, UserLoginProviders.EmailCode);

            var externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();
            if (externalLoginInfo != null && this.LinkExternalLogin)
            {
                return await this.CompleteExternalLoginLinkAsync(user, externalLoginInfo, returnUrl);
            }

            return LocalRedirect(returnUrl);
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
            this.RemoveEmailCodeValidationErrors();

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

        private async Task<IActionResult> StartEmailCodeAsync(string returnUrl)
        {
            returnUrl ??= Url.Content("~/");
            await this.LoadLoginPageDataAsync(returnUrl);
            ShowEmailCodeForm = true;
            EmailCodeInput ??= new EmailCodeInputModel();

            if (EmailCodeInput == null
                || string.IsNullOrWhiteSpace(EmailCodeInput.Email)
                || !new EmailAddressAttribute().IsValid(EmailCodeInput.Email))
            {
                ModelState.AddModelError(
                    "EmailCodeInput.Email",
                    this.pageLocalizer["EmailCodeEmailInvalid"].Value);
                return Page();
            }

            var user = await userManager.FindByEmailAsync(EmailCodeInput.Email);
            if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
            {
                // Do not reveal whether an email address belongs to an account.
                return Page();
            }

            DateTime now = DateTime.UtcNow;
            var previousState = HttpContext.Session.GetObjectFromJson<EmailCodeState>(EmailCodeSessionKey);
            if (previousState != null
                && string.Equals(
                    userManager.NormalizeEmail(previousState.Email),
                    userManager.NormalizeEmail(user.Email),
                    StringComparison.OrdinalIgnoreCase)
                && previousState.SentAtUtc > now.AddSeconds(-EmailCodeResendCooldownSeconds))
            {
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeResendTooSoon"].Value);
                return Page();
            }

            string code = RandomNumberGenerator
                .GetInt32(EmailCodeMinimum, EmailCodeMaximum)
                .ToString(CultureInfo.InvariantCulture);
            var state = new EmailCodeState
            {
                Email = user.Email,
                Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ExpiresAtUtc = now.AddMinutes(EmailCodeLifetimeMinutes),
                SentAtUtc = now,
                RememberMe = EmailCodeInput.RememberMe,
            };
            state.CodeHash = this.HashEmailCode(state.Salt, code);
            HttpContext.Session.SaveObjectAsJson(EmailCodeSessionKey, state);

            try
            {
                await emailSender.SendEmailAsync(
                    user.Email,
                    this.localizer["EmailLoginCodeSubject"].Value,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        this.localizer["EmailLoginCodeBody"].Value,
                        code));
            }
            catch (Exception ex)
            {
                HttpContext.Session.Remove(EmailCodeSessionKey);
                logger.LogError(ex, "Failed to send the email login code.");
                ModelState.AddModelError(string.Empty, this.pageLocalizer["EmailCodeSendFailed"].Value);
            }

            return Page();
        }

        private void RemoveEmailCodeValidationErrors()
        {
            foreach (string key in ModelState.Keys
                .Where(key => key == nameof(EmailLoginCode)
                    || key.StartsWith(nameof(EmailCodeInput) + ".", StringComparison.Ordinal))
                .ToList())
            {
                ModelState.Remove(key);
            }
        }

        private async Task LoadLoginPageDataAsync(string returnUrl)
        {
            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        private string HashEmailCode(string salt, string code)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(salt, ":", code))));
        }

        private bool IsEmailCodeValid(EmailCodeState state, string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
            {
                return false;
            }

            try
            {
                byte[] expected = Convert.FromBase64String(state.CodeHash);
                byte[] actual = Convert.FromBase64String(this.HashEmailCode(state.Salt, code));
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException)
            {
                return false;
            }
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
                logger.LogWarning(
                    "Could not link {Provider} during local account sign-in: {Errors}.",
                    externalLoginInfo.LoginProvider,
                    string.Join("; ", linkAttempt.Error.Errors.Select(error => error.Code)));
            }

            ModelState.AddModelError(
                string.Empty,
                this.pageLocalizer["LinkExternalLoginFailed", externalLoginInfo.ProviderDisplayName].Value);

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

        /// <summary>
        /// Email-code request model.
        /// </summary>
        public class EmailCodeInputModel
        {
            /// <summary>
            /// Gets or sets the email address.
            /// </summary>
            public string? Email { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the login should be remembered.
            /// </summary>
            public bool RememberMe { get; set; }
        }

        private sealed class EmailCodeState
        {
            public string Email { get; set; }

            public string Salt { get; set; }

            public string CodeHash { get; set; }

            public DateTime ExpiresAtUtc { get; set; }

            public DateTime SentAtUtc { get; set; }

            public bool RememberMe { get; set; }

            public int Attempts { get; set; }
        }
    }
}
