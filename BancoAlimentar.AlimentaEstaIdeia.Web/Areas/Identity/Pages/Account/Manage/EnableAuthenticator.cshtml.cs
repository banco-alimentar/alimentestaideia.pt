// -----------------------------------------------------------------------
// <copyright file="EnableAuthenticator.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Enable the authentication model.
    /// </summary>
    public class EnableAuthenticatorModel : PageModel
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        private readonly UserManager<WebUser> userManager;
        private readonly ILogger<EnableAuthenticatorModel> logger;
        private readonly UrlEncoder urlEncoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableAuthenticatorModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="urlEncoder">Url encoder service.</param>
        public EnableAuthenticatorModel(
            UserManager<WebUser> userManager,
            ILogger<EnableAuthenticatorModel> logger,
            UrlEncoder urlEncoder)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.urlEncoder = urlEncoder;
        }

        /// <summary>
        /// Gets or sets the shared key.
        /// </summary>
        public string SharedKey { get; set; }

        /// <summary>
        /// Gets or sets the authentication uri.
        /// </summary>
        public string AuthenticatorUri { get; set; }

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
        /// Gets or sets the input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

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

            await LoadSharedKeyAndQrCodeUriAsync(user);

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

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            // Strip spaces and hypens
            var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await userManager.VerifyTwoFactorTokenAsync(
                user, userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await userManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await userManager.GetUserIdAsync(user);
            logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

            StatusMessage = "Your authenticator app has been verified.";

            if (await userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                RecoveryCodes = recoveryCodes.ToArray();
                return RedirectToPage("./ShowRecoveryCodes");
            }
            else
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(WebUser user)
        {
            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);

            var email = await userManager.GetEmailAsync(user);
            AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                urlEncoder.Encode("BancoAlimentar.AlimentaEstaIdeia.Web"),
                urlEncoder.Encode(email),
                unformattedKey);
        }

        /// <summary>
        /// Input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets the 2f code.
            /// </summary>
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string Code { get; set; }
        }
    }
}
