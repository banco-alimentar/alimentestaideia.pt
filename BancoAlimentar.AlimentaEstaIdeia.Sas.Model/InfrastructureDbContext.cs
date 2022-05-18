// -----------------------------------------------------------------------
// <copyright file="InfrastructureDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// DbContext for the Doar SaS infrastructure.
    /// </summary>
    public class InfrastructureDbContext : IdentityDbContext<IdentityUser>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastructureDbContext"/> class.
        /// </summary>
        public InfrastructureDbContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastructureDbContext"/> class.
        /// </summary>
        /// <param name="options">DbContextOptions.</param>
        public InfrastructureDbContext(DbContextOptions<InfrastructureDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Tenant"/>.
        /// </summary>
        public DbSet<Tenant> Tenants { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="KeyVaultConfiguration"/>.
        /// </summary>
        public DbSet<KeyVaultConfiguration> KeyVaultConfigurations { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="DomainIdentifier"/>.
        /// </summary>
        public DbSet<DomainIdentifier> DomainIdentifiers { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="InvoiceConfiguration"/>.
        /// </summary>
        public DbSet<InvoiceConfiguration> InvoiceConfigurations { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="TenantConfiguration"/>.
        /// </summary>
        public DbSet<TenantConfiguration> TenantConfigurations { get; set; }
    }
}
