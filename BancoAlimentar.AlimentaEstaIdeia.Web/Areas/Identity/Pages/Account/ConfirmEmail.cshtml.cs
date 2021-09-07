// -----------------------------------------------------------------------
// <copyright file="ConfirmEmail.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account
{
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.WebUtilities;

    /// <summary>
    /// Confirmed email model.
    /// </summary>
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmEmailModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="localizer">Localizer.</param>
        public ConfirmEmailModel(UserManager<WebUser> userManager, IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="code">Email code.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? this.localizer["ConfirmEmailThanks"].Value : this.localizer["ConfirmEmailError"].Value;
            return RedirectToPage("/Donation");
        }
    }
}
