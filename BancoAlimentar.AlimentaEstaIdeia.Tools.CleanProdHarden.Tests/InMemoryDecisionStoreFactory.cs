// -----------------------------------------------------------------------
// <copyright file="InMemoryDecisionStoreFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// An in-memory SQLite decision store that lives as long as the factory.
    /// </summary>
    internal sealed class InMemoryDecisionStoreFactory : IDbContextFactory<DecisionStoreContext>, IDisposable
    {
        private readonly SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        private readonly DbContextOptions<DecisionStoreContext> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDecisionStoreFactory"/> class with an empty schema.
        /// </summary>
        public InMemoryDecisionStoreFactory()
        {
            this.connection.Open();
            this.options = new DbContextOptionsBuilder<DecisionStoreContext>().UseSqlite(this.connection).Options;
            using var db = new DecisionStoreContext(this.options);
            db.Database.EnsureCreated();
        }

        /// <inheritdoc/>
        public DecisionStoreContext CreateDbContext()
        {
            return new DecisionStoreContext(this.options);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.connection.Dispose();
        }
    }
}
