namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    public class RolesModel : PageModel
    {
        private readonly RoleManager<ApplicationRole> roleManager;

        public RolesModel(RoleManager<ApplicationRole> roleManager)
        {
            this.roleManager = roleManager;
        }

        public List<ApplicationRole> Roles { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadRoles();
            return Page();
        }

        private async Task LoadRoles()
        {
            Roles = await roleManager.Roles.ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> OnPostCreateNewRole(string roleName)
        {
            if (roleName != null)
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName.Trim()));
            }

            return Page();
        }
    }
}
