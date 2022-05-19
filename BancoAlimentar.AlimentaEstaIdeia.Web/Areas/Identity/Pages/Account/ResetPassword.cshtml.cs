// -----------------------------------------------------------------------
// <copyright file="ResetPassword.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Reset password.
/// </summary>
[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<WebUser> userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordModel"/> class.
    /// </summary>
    /// <param name="userManager">User Manager.</param>
    public ResetPasswordModel(UserManager<WebUser> userManager)
    {
        this.userManager = userManager;
    }

    /// <summary>
    /// Gets or sets the input model.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <param name="code">Code.</param>
    /// <returns>Page.</returns>
    public IActionResult OnGet(string code = null)
    {
        if (code == null)
        {
            return BadRequest("A code must be supplied for password reset.");
        }
        else
        {
            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)),
            };
            return Page();
        }
    }

    /// <summary>
    /// Execute post operation.
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
            // Don't reveal that the user does not exist
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
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
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the confirm password.
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        public string Code { get; set; }
    }
}
