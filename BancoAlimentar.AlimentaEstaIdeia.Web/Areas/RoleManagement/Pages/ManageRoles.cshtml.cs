// -----------------------------------------------------------------------
// <copyright file="ManageRoles.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Model;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class ManageRolesModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageRolesModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="roleManager"></param>
        public ManageRolesModel(UserManager<WebUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            ManageUserRolesViewModels = new List<ManageUserRolesViewModel>();
        }

        public List<ManageUserRolesViewModel> ManageUserRolesViewModels { get; set; }

        public string UserId { get; set; }

        public string Name { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string userId)
        {
            UserId = userId;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Page();
            }

            Name = user.Email;

            ManageUserRolesViewModels = new List<ManageUserRolesViewModel>();
            foreach (var role in roleManager.Roles.ToList())
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                };
                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }

                ManageUserRolesViewModels.Add(userRolesViewModel);
            }

            return Page();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="manageUserRolesViewModels"></param>
        /// <param name="userId"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostSave([FromForm] List<ManageUserRolesViewModel> manageUserRolesViewModels, string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Page();
            }

            var roles = await userManager.GetRolesAsync(user);
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Cannot remove user existing roles");
                return Page();
            }

            result = await userManager.AddToRolesAsync(user, manageUserRolesViewModels.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Cannot add selected roles to user");
                return Page();
            }

            return RedirectToPage("UserRoles");
        }
    }
}
