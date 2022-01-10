// -----------------------------------------------------------------------
// <copyright file="ResendEmailConfirmation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.ComponentModel.DataAnnotations;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.WebUtilities;

    /// <summary>
    /// Resend email confirmation model.
    /// </summary>
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResendEmailConfirmationModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="localizer">Localizer.</param>
        public ResendEmailConfirmationModel(
            UserManager<WebUser> userManager,
            IEmailSender emailSender,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email de verificação enviado. Por favor verifique o seu email.");
                return Page();
            }

            var userId = await userManager.GetUserIdAsync(user);
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId, code },
                protocol: Request.Scheme);
            await emailSender.SendEmailAsync(
                            Input.Email,
                            this.localizer["ConfirmEmailSubject"].Value,
                            string.Format(localizer["ConfirmEmailBody"].Value, HtmlEncoder.Default.Encode(callbackUrl)));

            ModelState.AddModelError(string.Empty, "Email de verificação enviado. Por favor verifique o seu email.");
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
        }
    }
}
