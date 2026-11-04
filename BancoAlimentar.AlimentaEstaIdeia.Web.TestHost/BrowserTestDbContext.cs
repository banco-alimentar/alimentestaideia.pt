// -----------------------------------------------------------------------
// <copyright file="BrowserTestDbContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// SQLite compatibility model for browser fixtures, never registered by the deployed application.
    /// </summary>
    internal sealed class BrowserTestDbContext : ApplicationDbContext
    {
        /// <summary>Initializes a new instance of the <see cref="BrowserTestDbContext"/> class.</summary>
        /// <param name="options">Test-owned SQLite options.</param>
        public BrowserTestDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Referral lookups normalize the supplied code; the deployed SQL catalog is case-insensitive.
            // Keep this test compatibility choice out of the production model and migration snapshot.
            modelBuilder.Entity<Referral>().Property(value => value.Code).UseCollation("NOCASE");
        }
    }
}
