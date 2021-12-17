// -----------------------------------------------------------------------
// <copyright file="UserRoles.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Model;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// User roles model.
    /// </summary>
    public class UserRolesModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRolesModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="roleManager">Role manager.</param>
        public UserRolesModel(
            UserManager<WebUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        /// <summary>
        /// Gets or sets the collection of <see cref="UserRolesViewModel"/>.
        /// </summary>
        public List<UserRolesViewModel> UserRolesViewModel { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            var users = await userManager.Users.ToListAsync();
            UserRolesViewModel = new List<UserRolesViewModel>();
            foreach (WebUser user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.FirstName = user.FullName;
                thisViewModel.Roles = await GetUserRoles(user);
                UserRolesViewModel.Add(thisViewModel);
            }

            return Page();
        }

        private async Task<List<string>> GetUserRoles(WebUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}
