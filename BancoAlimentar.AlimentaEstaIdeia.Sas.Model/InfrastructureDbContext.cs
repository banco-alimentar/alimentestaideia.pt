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
                new KeyVaultConfiguration(1, new Uri("doarbancoalimentar"), DateTime.UtcNow, "Development", 7),
                new KeyVaultConfiguration(7, new Uri("doarbancoalimentar"), DateTime.UtcNow, "Development", 5),
                new KeyVaultConfiguration(8, new Uri("doaralimentestaideia"), DateTime.UtcNow, "Development", 10),
                new KeyVaultConfiguration(9, new Uri("doaralimentestaideia"), DateTime.UtcNow, "Development", 13),
                new KeyVaultConfiguration(10, new Uri("doaralimentestaideia"), DateTime.UtcNow, "Staging", 10),
                new KeyVaultConfiguration(11, new Uri("doarbancoalimentar"), DateTime.UtcNow, "Staging", 7),
                new KeyVaultConfiguration(12, new Uri("alimentaestaideia-key"), DateTime.UtcNow, "Staging", 14));

            modelBuilder.Entity<DomainIdentifier>().HasData(
                new DomainIdentifier(3, "localhost", "Development", DateTime.UtcNow, 5),
                new DomainIdentifier(4, "doar-dev.bancoalimentar.pt", "Development", DateTime.UtcNow, 7),
                new DomainIdentifier(5, "alimentaestaideia-beta.azurewebsites.net", "Staging", DateTime.UtcNow, 10),
                new DomainIdentifier(6, "doar-dev.alimentestaideia.pt", "Staging", DateTime.UtcNow, 10),
                new DomainIdentifier(7, "alimentaestaideia-developer.azurewebsites.net", "Staging", DateTime.UtcNow, 14),
                new DomainIdentifier(8, "alimentaestaideia-developer.azurewebsites.net", "Development", DateTime.UtcNow, 14),
                new DomainIdentifier(9, "doar-dev.bancoalimentar.pt", "Staging", DateTime.UtcNow, 7));

            modelBuilder.Entity<Tenant>().HasData(
                new Tenant("localhost", new Guid("9d46682c-588b-45ce-8829)-f8ce771dc10e"), DateTime.UtcNow, InvoicingStrategy.MultipleTablesPerFoodBank, PaymentStrategy.IndividualPaymentProcessorPerFoodBank, null),
                new Tenant("bancoalimentar", new Guid("2d4d6448-71d3-454a-a584-9ebfc0b7ede5"), DateTime.UtcNow, InvoicingStrategy.MultipleTablesPerFoodBank, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant("alimentaestaideia-beta", new Guid("bd31d165-b8df-4c7a-a5e3-5e3d155948e2"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant("alimentaestaideia-beta", new Guid("de68a683-0cd2-44ce-b9c6-505aabbcdfc3"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant("doar-dev.alimentestaideia.pt", new Guid("03317653-9140-4cc0-91e0-2b2aa2a8e5fe"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant("doar-dev.alimentestaideia-dev.pt", new Guid("f3aea354-b2ad-4451-893f-891dfb2c6c99"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null),
                new Tenant("alimentaestaideia-developer.azurewebsites.net", new Guid("f904b771-b750-4392-a8f6-f76a8b9cc1be"), DateTime.UtcNow, InvoicingStrategy.SingleInvoiceTable, PaymentStrategy.SharedPaymentProcessor, null));
        }
    }
}
