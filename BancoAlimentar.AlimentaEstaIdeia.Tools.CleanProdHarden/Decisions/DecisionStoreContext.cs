// -----------------------------------------------------------------------
// <copyright file="DecisionStoreContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Decisions
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Local SQLite store for reviewer decisions. It never touches the target database.
    /// </summary>
    public class DecisionStoreContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecisionStoreContext"/> class.
        /// </summary>
        /// <param name="options">Options.</param>
        public DecisionStoreContext(DbContextOptions<DecisionStoreContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets the decisions.
        /// </summary>
        public DbSet<GroupDecisionRecord> Decisions => this.Set<GroupDecisionRecord>();

        /// <summary>
        /// Gets the history.
        /// </summary>
        public DbSet<DecisionEventRecord> Events => this.Set<DecisionEventRecord>();

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GroupDecisionRecord>(entity =>
            {
                entity.HasIndex(record => new { record.TargetKey, record.GroupId }).IsUnique();
                entity.Property(record => record.Action).HasConversion<string>();
                entity.Property(record => record.Status).HasConversion<string>();
            });

            modelBuilder.Entity<DecisionEventRecord>(entity =>
            {
                entity.HasIndex(record => new { record.TargetKey, record.GroupId, record.AtUtc });
                entity.Property(record => record.Action).HasConversion<string>();
                entity.Property(record => record.Kind).HasConversion<string>();
            });
        }
    }
}
