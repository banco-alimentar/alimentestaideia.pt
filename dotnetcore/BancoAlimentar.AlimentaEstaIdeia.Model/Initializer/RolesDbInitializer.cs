using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System.Threading.Tasks;

    public class RolesDbInitializer
    {
        public static async Task SeedRolesAsync(UserManager<WebUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            await roleManager.CreateAsync(new ApplicationRole(UserRoles.SuperAdmin.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(UserRoles.Admin.ToString()));
        }
    }
}
