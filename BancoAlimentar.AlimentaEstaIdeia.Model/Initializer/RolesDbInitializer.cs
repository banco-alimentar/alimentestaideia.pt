// -----------------------------------------------------------------------
// <copyright file="RolesDbInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Intialize the default Roles in the database.
    /// </summary>
    public class RolesDbInitializer
    {
        /// <summary>
        /// Feed the default roles in the database.
        /// </summary>
        /// <param name="userManager">A reference to the UserManager.</param>
        /// <param name="roleManager">A reference to the RoleManager.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public static async Task SeedRolesAsync(UserManager<WebUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            await roleManager.CreateAsync(new ApplicationRole(UserRoles.SuperAdmin.ToString()));
            await roleManager.CreateAsync(new ApplicationRole(UserRoles.Admin.ToString()));

            WebUser guerrerotook = await userManager.FindByEmailAsync("guerrerotook@outlook.com");
            if (guerrerotook != null)
            {
                await userManager.AddToRoleAsync(guerrerotook, UserRoles.Admin.ToString());
                await userManager.AddToRoleAsync(guerrerotook, UserRoles.SuperAdmin.ToString());
            }

            WebUser tiagoand = await userManager.FindByEmailAsync("tiago.andradesilva@bancoalimentar.pt");
            if (tiagoand != null)
            {
                await userManager.AddToRoleAsync(tiagoand, UserRoles.Admin.ToString());
                await userManager.AddToRoleAsync(tiagoand, UserRoles.SuperAdmin.ToString());
            }
        }
    }
}
