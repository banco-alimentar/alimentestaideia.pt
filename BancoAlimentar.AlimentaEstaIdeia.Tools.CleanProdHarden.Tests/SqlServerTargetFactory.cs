// -----------------------------------------------------------------------
// <copyright file="SqlServerTargetFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// A target-database factory for a real SQL Server.
    /// </summary>
    internal sealed class SqlServerTargetFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerTargetFactory"/> class.
        /// </summary>
        /// <param name="connectionString">SQL Server connection string.</param>
        public SqlServerTargetFactory(string connectionString)
        {
            this.options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString, sql => sql.CommandTimeout(120))
                .Options;
        }

        /// <inheritdoc/>
        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(this.options);
        }
    }
}
