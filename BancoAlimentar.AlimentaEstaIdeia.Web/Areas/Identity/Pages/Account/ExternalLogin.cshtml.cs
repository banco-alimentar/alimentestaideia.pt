﻿// -----------------------------------------------------------------------
// <copyright file="ExternalLogin.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;

    /// <summary>
    /// External login model.
    /// </summary>
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly IUnitOfWork context;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;
        private readonly ILogger<ExternalLoginModel> logger;
        private readonly IReadOnlyDictionary<string, string> claimsToSync =
            new Dictionary<string, string>()
            {
                { "urn:google:picture", "headshot.png" },
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalLoginModel"/> class.
        /// </summary>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="localizer">Localizer.</param>
        public ExternalLoginModel(
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            IUnitOfWork context,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
            this.emailSender = emailSender;
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the Input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets the provider display name.
        /// </summary>
        public string ProviderDisplayName { get; set; }

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
        /// Execute the get operation.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>Page.</returns>
        public IActionResult OnGetAsync(string provider = null, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(provider) && string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToPage("./Login");
            }
            else
            {
                var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return new ChallengeResult(provider, properties);
            }
        }

        /// <summary>
        /// Executed the post operation.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>Page.</returns>
        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Execute the get callback operation.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <param name="remoteError">Call back remote error.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";

                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a
            // login.
            var result = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            // if (info.LoginProvider == "Microsoft")
            // {
            //    await GetMicrosoftAccountInformation(info, info.Principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"));
            // }
            if (result.Succeeded)
            {
                logger.LogInformation(
                    "{Name} logged in with {LoginProvider} provider.",
                    info.Principal.Identity.Name,
                    info.LoginProvider);

                bool refreshSignIn = false;

                var user = await userManager.FindByLoginAsync(
                    info.LoginProvider,
                    info.ProviderKey);

                if (user.UserName != info.Principal.Identity.Name)
                {
                    user.UserName = info.Principal.Identity.Name;
                    refreshSignIn = true;
                }

                if (claimsToSync.Count > 0)
                {
                    var userClaims = await userManager.GetClaimsAsync(user);

                    foreach (var addedClaim in claimsToSync)
                    {
                        var userClaim = userClaims
                            .FirstOrDefault(c => c.Type == addedClaim.Key);

                        if (info.Principal.HasClaim(c => c.Type == addedClaim.Key))
                        {
                            var externalClaim = info.Principal.FindFirst(addedClaim.Key);

                            if (userClaim == null)
                            {
                                await userManager.AddClaimAsync(
                                    user,
                                    new Claim(addedClaim.Key, externalClaim.Value));
                                refreshSignIn = true;
                            }
                            else if (userClaim.Value != externalClaim.Value)
                            {
                                await userManager
                                    .ReplaceClaimAsync(user, userClaim, externalClaim);
                                refreshSignIn = true;
                            }
                        }
                        else if (userClaim == null)
                        {
                            // Fill with a default value
                            await userManager.AddClaimAsync(user, new Claim(
                                addedClaim.Key,
                                addedClaim.Value));
                            refreshSignIn = true;
                        }
                    }
                }

                if (refreshSignIn)
                {
                    await signInManager.RefreshSignInAsync(user);
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
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                    };
                }

                return Page();
            }
        }

        /// <summary>
        /// Execute the post confirmation operation.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            // Get the information about the user from the external login provider
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = new WebUser { UserName = Input.Email, Email = Input.Email };

                var result = await userManager.CreateAsync(user);

                if (result.Succeeded || (!result.Succeeded && result.Errors.FirstOrDefault().Code == "DuplicateEmail"))
                {
                    if (!result.Succeeded && result.Errors.FirstOrDefault().Code == "DuplicateEmail")
                    {
                        user = await userManager.FindByEmailAsync(Input.Email);
                    }

                    result = await userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        var phoneNumber = await userManager.GetPhoneNumberAsync(user);
                        if (Input.PhoneNumber != phoneNumber)
                        {
                            var setPhoneResult = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                        }

                        WebUser webUser = this.context.User.FindUserById(user.Id);
                        webUser.PhoneNumber = Input.PhoneNumber;
                        webUser.Nif = Input.Nif;
                        webUser.CompanyName = Input.CompanyName;
                        webUser.Address = Input.Address;
                        webUser.FullName = Input.FullName;

                        context.User.Modify(webUser);
                        context.Complete();

                        this.context.Donation.ClaimDonationToUser(HttpContext.Session.GetString(RegisterModel.PublicDonationIdKey), webUser);

                        if (info.Principal.HasClaim(c => c.Type == ClaimTypes.GivenName))
                        {
                            await userManager.AddClaimAsync(
                                user,
                                info.Principal.FindFirst(ClaimTypes.GivenName));
                        }

                        if (info.Principal.HasClaim(c => c.Type == "urn:google:locale"))
                        {
                            await userManager.AddClaimAsync(
                                user,
                                info.Principal.FindFirst("urn:google:locale"));
                        }

                        if (info.Principal.HasClaim(c => c.Type == "urn:google:picture"))
                        {
                            await userManager.AddClaimAsync(
                                user,
                                info.Principal.FindFirst("urn:google:picture"));
                        }

                        // Include the access token in the properties
                        var props = new AuthenticationProperties();
                        props.StoreTokens(info.AuthenticationTokens);
                        props.IsPersistent = true;

                        logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await userManager.GetUserIdAsync(user);
                        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId, code },
                            protocol: Request.Scheme);

                        await emailSender.SendEmailAsync(
                            Input.Email,
                            this.localizer["ConfirmEmailSubject"].Value,
                            string.Format(localizer["ConfirmEmailBody"].Value, HtmlEncoder.Default.Encode(callbackUrl)));

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Input.Email });
                        }

                        await signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

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

        // private async Task GetMicrosoftAccountInformation(ExternalLoginInfo info, string email)
        // {
        //    if (info != null)
        //    {
        //        AuthenticationToken accessToken = info.AuthenticationTokens
        //            .Where(p => p.Name.ToLowerInvariant() == "access_token")
        //            .FirstOrDefault();
        //        if (accessToken != null)
        //        {
        //            IAuthenticationProvider authentication = new AccessTokenAuthenticationProvider(accessToken.Value);
        //            GraphServiceClient client = new GraphServiceClient(authentication);
        //            var me = await client.Me
        //                .Request()
        //                .GetAsync();
        //            // var profilePhoto = await client.Users[me.Id].Photo.Request().GetAsync();
        //            // var stream = await client.Users[me.Id].Photo.Content.Request().GetAsync();
        //        }
        //    }
        // }

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
            /// Gets or sets the phone number.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets the nif.
            /// </summary>
            [Display(Name = "Nif")]
            public string Nif { get; set; }

            /// <summary>
            /// Gets or sets the company name.
            /// </summary>
            [Display(Name = "Company Name")]
            public string CompanyName { get; set; }

            /// <summary>
            /// Gets or sets the donor address.
            /// </summary>
            [Display(Name = "Address")]
            public DonorAddress Address { get; set; }

            /// <summary>
            /// Gets or sets the full name.
            /// </summary>
            [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameRequired")]
            [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
            [DisplayAttribute(Name = "Nome")]
            [BindProperty]
            public string FullName { get; set; }
        }
    }
}
