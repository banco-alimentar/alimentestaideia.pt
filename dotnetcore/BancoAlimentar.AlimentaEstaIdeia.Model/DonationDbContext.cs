// <copyright file="DonationDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default <see cref="DbContext"/> for the database. Using SQL Server.
    /// </summary>
    public class DonationDbContext : DbContext
    {
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
    }
}
