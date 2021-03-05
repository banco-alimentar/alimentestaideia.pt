﻿namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;

    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<WebUser> _signInManager;
        private readonly UserManager<WebUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        private readonly IReadOnlyDictionary<string, string> _claimsToSync =
            new Dictionary<string, string>()
            {
                { "urn:google:picture", "headshot.png" },
            };

        public ExternalLoginModel(
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGetAsync(string provider = null, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(provider) && string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToPage("./Login");
            }
            else
            {
                var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
                var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return new ChallengeResult(provider, properties);
            }
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        private async Task GetMicrosoftAccountInformation(ExternalLoginInfo info, string email)
        {
            if (info != null)
            {
                AuthenticationToken accessToken = info.AuthenticationTokens
                    .Where(p => p.Name.ToLowerInvariant() == "access_token")
                    .FirstOrDefault();

                if (accessToken != null)
                {
                    IAuthenticationProvider authentication = new AccessTokenAuthenticationProvider(accessToken.Value);
                    GraphServiceClient client = new GraphServiceClient(authentication);
                    var me = await client.Me
                        .Request()
                        .GetAsync();

                    //var profilePhoto = await client.Users[me.Id].Photo.Request().GetAsync();
                    //var stream = await client.Users[me.Id].Photo.Content.Request().GetAsync();
                }
            }
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";

                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a 
            // login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider,
                info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (info.LoginProvider == "Microsoft")
            {
                await GetMicrosoftAccountInformation(info, info.Principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"));
            }

            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.",
                    info.Principal.Identity.Name, info.LoginProvider);

                bool refreshSignIn = false;

                var user = await _userManager.FindByLoginAsync(info.LoginProvider,
                       info.ProviderKey);

                if (user.UserName != info.Principal.Identity.Name)
                {
                    user.UserName = info.Principal.Identity.Name;
                    refreshSignIn = true;
                }

                if (_claimsToSync.Count > 0)
                {
                    var userClaims = await _userManager.GetClaimsAsync(user);

                    foreach (var addedClaim in _claimsToSync)
                    {
                        var userClaim = userClaims
                            .FirstOrDefault(c => c.Type == addedClaim.Key);

                        if (info.Principal.HasClaim(c => c.Type == addedClaim.Key))
                        {
                            var externalClaim = info.Principal.FindFirst(addedClaim.Key);

                            if (userClaim == null)
                            {
                                await _userManager.AddClaimAsync(user,
                                    new Claim(addedClaim.Key, externalClaim.Value));
                                refreshSignIn = true;
                            }
                            else if (userClaim.Value != externalClaim.Value)
                            {
                                await _userManager
                                    .ReplaceClaimAsync(user, userClaim, externalClaim);
                                refreshSignIn = true;
                            }
                        }
                        else if (userClaim == null)
                        {
                            // Fill with a default value
                            await _userManager.AddClaimAsync(user, new Claim(addedClaim.Key,
                                addedClaim.Value));
                            refreshSignIn = true;
                        }
                    }
                }

                if (refreshSignIn)
                {
                    await _signInManager.RefreshSignInAsync(user);
                }

                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an 
                // account.
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;

                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel
                    {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                    };
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = new WebUser { UserName = Input.Email, Email = Input.Email };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        if (info.Principal.HasClaim(c => c.Type == ClaimTypes.GivenName))
                        {
                            await _userManager.AddClaimAsync(user,
                                info.Principal.FindFirst(ClaimTypes.GivenName));
                        }

                        if (info.Principal.HasClaim(c => c.Type == "urn:google:locale"))
                        {
                            await _userManager.AddClaimAsync(user,
                                info.Principal.FindFirst("urn:google:locale"));
                        }

                        if (info.Principal.HasClaim(c => c.Type == "urn:google:picture"))
                        {
                            await _userManager.AddClaimAsync(user,
                                info.Principal.FindFirst("urn:google:picture"));
                        }

                        // Include the access token in the properties
                        var props = new AuthenticationProperties();
                        props.StoreTokens(info.AuthenticationTokens);
                        props.IsPersistent = true;

                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
