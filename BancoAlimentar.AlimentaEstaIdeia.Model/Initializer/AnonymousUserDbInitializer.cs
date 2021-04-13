// <copyright file="AnonymousUserDbInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Initialize the WebUser table when migration.
    /// </summary>
    public static class AnonymousUserDbInitializer
    {
        /// <summary>
        /// Initialize the database.
        /// </summary>
        /// <param name="context">A reference to the <see cref="ApplicationDbContext"/>.</param>
        public static void Initialize(ApplicationDbContext context)
        {
            string zeroId = default(Guid).ToString();
            WebUser user = context.WebUser.Where(p => p.Id == zeroId).FirstOrDefault();
            if (user == null)
            {
                user = new WebUser()
                {
                    Id = zeroId,
                    Email = "noemail@email.com",
                    UserName = "AnonymousUser",
                };

                context.WebUser.Add(user);
                context.SaveChanges();
            }
        }
    }
}
