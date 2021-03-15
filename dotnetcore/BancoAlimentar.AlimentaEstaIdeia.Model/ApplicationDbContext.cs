// <copyright file="ApplicationDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default DbContext for the project.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">DbContextOptions.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="WebUser"/>.
        /// </summary>
        public DbSet<WebUser> WebUser { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Donation"/>.
        /// </summary>
        public DbSet<Donation> Donations { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="DonationItem"/>.
        /// </summary>
        public DbSet<DonationItem> DonationItems { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="DonorAddress"/>.
        /// </summary>
        public DbSet<DonorAddress> DonorAddresses { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="FoodBank"/>.
        /// </summary>
        public DbSet<FoodBank> FoodBanks { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="ProductCatalogue"/>.
        /// </summary>
        public DbSet<ProductCatalogue> ProductCatalogues { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Invoice"/>.
        /// </summary>
        public DbSet<Invoice> Invoices { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="PayPalPayment"/>.
        /// </summary>
        public DbSet<PayPalPayment> PayPalPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="MultiBankPayment"/>.
        /// </summary>
        public DbSet<MultiBankPayment> MultiBankPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="CreditCardPayment"/>.
        /// </summary>
        public DbSet<CreditCardPayment> CreditCardPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="MBWayPayment"/>.
        /// </summary>
        public DbSet<MBWayPayment> MBWayPayments { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for the <see cref="Campaign"/>.
        /// </summary>
        public DbSet<Campaign> Campaigns { get; set; }
    }
}
