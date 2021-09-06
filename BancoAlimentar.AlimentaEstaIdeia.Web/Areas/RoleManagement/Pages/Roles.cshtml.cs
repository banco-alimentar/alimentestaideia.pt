// -----------------------------------------------------------------------
// <copyright file="Roles.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    public class RolesModel : PageModel
    {
        private readonly RoleManager<ApplicationRole> roleManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesModel"/> class.
        /// </summary>
        /// <param name="roleManager"></param>
        public RolesModel(RoleManager<ApplicationRole> roleManager)
        {
            this.roleManager = roleManager;
        }

        public List<ApplicationRole> Roles { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            await LoadRoles();
            return Page();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostCreateNewRole(string roleName)
        {
            if (roleName != null)
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName.Trim()));
            }

            return Page();
        }

        private async Task LoadRoles()
        {
            Roles = await roleManager.Roles.ToListAsync();
        }
    }
}
