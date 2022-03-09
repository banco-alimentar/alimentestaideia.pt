// -----------------------------------------------------------------------
// <copyright file="TenantDbInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Initializer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Initialize the Tenant table when migration.
    /// </summary>
    public static class TenantDbInitializer
    {
        /// <summary>
        /// Initialize the database.
        /// </summary>
        /// <param name="context">A reference to the <see cref="InfrastructureDbContext"/>.</param>
        public static void Initialize(InfrastructureDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Tenants.Any())
            {
                context.Tenants.Add(new Tenant()
                {
                    Created = DateTime.UtcNow,
                    DomainIdentifier = "localhost",
                    Name = "Default-localhost",
                    PublicId = Guid.NewGuid(),
                    KeyVaultConfigurations = new List<KeyVaultConfiguration>()
                    {
                        new KeyVaultConfiguration()
                        {
                            Created = DateTime.UtcNow,
                            Environment = "Development",
                            Vault = new Uri("https://vault.net"),
                        },
                    },
                });
            }

            context.SaveChanges();
        }
    }
}
