// -----------------------------------------------------------------------
// <copyright file="InitDatabase.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// This is a helper class for seeding database with initial data.
    /// </summary>
    public class InitDatabase
    {
        /// <summary>
        /// This method seeds database with initial data.
        /// </summary>
        /// <param name="context">DB context object.</param>
        /// <param name="userManager">UserManager object.</param>
        /// <param name="roleManager">RoleManager object.</param>
        /// <returns>Returns a task.</returns>
        public static async Task Seed(ApplicationDbContext context, UserManager<WebUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            ProductCatalogueDbInitializer.Initialize(context);
            AnonymousUserDbInitializer.Initialize(context);
            FoodBankDbInitializer.Initialize(context);

            await RolesDbInitializer.SeedRolesAsync(userManager, roleManager);
        }
    }
}
