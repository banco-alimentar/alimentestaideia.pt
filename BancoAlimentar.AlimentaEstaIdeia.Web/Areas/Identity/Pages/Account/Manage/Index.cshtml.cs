// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Index page model.
    /// </summary>
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly SignInManager<WebUser> signInManager;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="context">Unit of work.</param>
        public IndexModel(
            UserManager<WebUser> userManager,
            SignInManager<WebUser> signInManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the Status Message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets input model.
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

            await LoadAsync(user);
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
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            WebUser webUser = this.context.User.FindUserById(user.Id);
            webUser.PhoneNumber = Input.PhoneNumber;
            webUser.Nif = Input.Nif;
            webUser.CompanyName = Input.CompanyName;
            webUser.Address = Input.Address;
            webUser.FullName = Input.FullName;

            context.User.Modify(webUser);
            context.Complete();

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        private async Task LoadAsync(WebUser user)
        {
            var userName = await userManager.GetUserNameAsync(user);
            var phoneNumber = await userManager.GetPhoneNumberAsync(user);
            WebUser webUser = this.context.User.FindUserById(user.Id);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Nif = webUser.Nif,
                CompanyName = webUser.CompanyName,
                Address = webUser.Address == null ? new DonorAddress() : webUser.Address,
                FullName = webUser.FullName,
            };
        }

        /// <summary>
        /// Input model.
        /// </summary>
        public class InputModel
        {
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
            /// Gets or sets the address.
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
