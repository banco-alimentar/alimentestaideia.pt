// -----------------------------------------------------------------------
// <copyright file="Register.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
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

    /// <summary>
    /// Register model.
    /// </summary>
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        /// <summary>
        /// Public donation id key.
        /// </summary>
        public const string PublicDonationIdKey = "PublicDonationId";

        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly ILogger<RegisterModel> logger;
        private readonly IEmailSender emailSender;
        private readonly IUnitOfWork context;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="localizer">Localizer.</param>
        public RegisterModel(
            UserManager<WebUser> userManager,
            SignInManager<WebUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IUnitOfWork context,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.emailSender = emailSender;
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets the return url.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets the external logins authentication schemes.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="returnUrl">Return url.</param>
        /// <param name="publicDonationId">Public donation id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync(string returnUrl = null, string publicDonationId = null)
        {
            ReturnUrl = returnUrl;
            if (!string.IsNullOrEmpty(publicDonationId))
            {
                HttpContext.Session.SetString(PublicDonationIdKey, publicDonationId);
            }

            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
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
            if (ModelState.IsValid && new Validation.NifAttribute().IsValid(Input.Nif))
            {
                var user = new WebUser { UserName = Input.Email, Email = Input.Email };
                var result = await userManager.CreateAsync(user, Input.Password);
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

                    this.context.Donation.ClaimDonationToUser(HttpContext.Session.GetString(PublicDonationIdKey), webUser);

                    logger.LogInformation("User created a new account with password.");

                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code, returnUrl },
                        protocol: Request.Scheme);

                    await emailSender.SendEmailAsync(
                            Input.Email,
                            this.localizer["ConfirmEmailSubject"].Value,
                            string.Format(localizer["ConfirmEmailBody"].Value, HtmlEncoder.Default.Encode(callbackUrl)));

                    if (userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                    }
                    else
                    {
                        await signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                ModelState.AddModelError("Nif", "Invalid Nif");
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
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the password.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets the confirm password.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

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
