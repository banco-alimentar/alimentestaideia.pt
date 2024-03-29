﻿// -----------------------------------------------------------------------
// <copyright file="ForgotPassword.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Forgot password model.
    /// </summary>
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IEmailSender emailSender;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForgotPasswordModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="localizer">Localizer.</param>
        public ForgotPasswordModel(UserManager<WebUser> userManager, IEmailSender emailSender, IHtmlLocalizer<IdentitySharedResources> localizer)
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
        /// Execute the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation", "WithEmailNotConfirmedNote");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await emailSender.SendEmailAsync(
                    Input.Email,
                    this.localizer["ResetPassword"].Value,
                    $"{this.localizer["ResetPasswordMessage"].Value} <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>{this.localizer["ResetPasswordClickHere"].Value}</a>.");

                return RedirectToPage("./ForgotPasswordConfirmation");
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
        }
    }
}
