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

        /// <summary>
        /// Seed the database.
        /// </summary>
        /// <param name="modelBuilder">Model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KeyVaultConfiguration>().HasData(
                new { Id = 1, Vault = new Uri("doarbancoalimentar", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Development", TenantId = 7 },
                new { Id = 7, Vault = new Uri("doarbancoalimentar", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Development", TenantId = 5 },
                new { Id = 8, Vault = new Uri("doaralimentestaideia", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Development", TenantId = 10 },
                new { Id = 9, Vault = new Uri("doaralimentestaideia", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Development", TenantId = 13 },
                new { Id = 10, Vault = new Uri("doaralimentestaideia", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Staging", TenantId = 10 },
                new { Id = 11, Vault = new Uri("doarbancoalimentar", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Staging", TenantId = 7 },
                new { Id = 12, Vault = new Uri("alimentaestaideia-key", UriKind.Relative), Created = DateTime.UtcNow, Environment = "Staging", TenantId = 14 });

            modelBuilder.Entity<DomainIdentifier>().HasData(
                new { Id = 3, DomainName = "localhost", Environment = "Development", Created = DateTime.UtcNow, TenantId = 5 },
                new { Id = 4, DomainName = "doar-dev.bancoalimentar.pt", Environment = "Development", Created = DateTime.UtcNow, TenantId = 7 },
                new { Id = 5, DomainName = "alimentaestaideia-beta.azurewebsites.net", Environment = "Staging", Created = DateTime.UtcNow, TenantId = 10 },
                new { Id = 6, DomainName = "doar-dev.alimentestaideia.pt", Environment = "Staging", Created = DateTime.UtcNow, TenantId = 10 },
                new { Id = 7, DomainName = "alimentaestaideia-developer.azurewebsites.net", Environment = "Staging", Created = DateTime.UtcNow, TenantId = 14 },
                new { Id = 8, DomainName = "alimentaestaideia-developer.azurewebsites.net", Environment = "Development", Created = DateTime.UtcNow, TenantId = 14 },
                new { Id = 9, DomainName = "doar-dev.bancoalimentar.pt", Environment = "Staging", Created = DateTime.UtcNow, TenantId = 7 });

            modelBuilder.Entity<Tenant>().HasData(
                new Tenant(5, "localhost", new Guid("9d46682c-588b-45ce-8829-f8ce771dc10e"), DateTime.UtcNow, InvoicingStrategy.MultipleTablesPerFoodBank, PaymentStrategy.IndividualPaymentProcessorPerFoodBank, null),
                new Tenant(7, "bancoalimentar", new Guid("2d4d6448-71d3-454a-a584-9ebfc0b7ede5"), DateTime.UtcNow, InvoicingStrategy.MultipleTablesPerFoodBank, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant(8, "alimentaestaideia-beta", new Guid("bd31d165-b8df-4c7a-a5e3-5e3d155948e2"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant(9, "alimentaestaideia-beta", new Guid("de68a683-0cd2-44ce-b9c6-505aabbcdfc3"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant(10, "doar-dev.alimentestaideia.pt", new Guid("03317653-9140-4cc0-91e0-2b2aa2a8e5fe"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant(13, "doar-dev.alimentestaideia-dev.pt", new Guid("f3aea354-b2ad-4451-893f-891dfb2c6c99"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant(14, "alimentaestaideia-developer.azurewebsites.net", new Guid("f904b771-b750-4392-a8f6-f76a8b9cc1be"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null));

            base.OnModelCreating(modelBuilder);
        }
    }
}
