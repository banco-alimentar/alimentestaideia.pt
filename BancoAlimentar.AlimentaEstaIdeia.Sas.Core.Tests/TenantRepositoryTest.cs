namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;
    using Microsoft.Extensions.Caching.Memory;
    using Xunit;

    /// <summary>
    /// Tests domain lookup cache isolation and infrastructure schema constraints.
    /// </summary>
    public class TenantRepositoryTest
    {
        /// <summary>
        /// Tenant cache entries include their environment and do not collide with other cache users.
        /// </summary>
        [Fact]
        public void DomainLookupCacheIncludesEnvironment()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            using var context = new InfrastructureDbContext(new DbContextOptionsBuilder<InfrastructureDbContext>().UseSqlite(connection).Options);
            context.Database.EnsureCreated();
            context.Tenants.AddRange(CreateTenant("development", "Development"), CreateTenant("production", "Production"));
            context.SaveChanges();
            context.ChangeTracker.Clear();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            cache.Set("tenant.example", "unrelated cache entry");
            using var telemetry = new TelemetryConfiguration { DisableTelemetry = true };
            var repository = new TenantRepository(context, cache, new TelemetryClient(telemetry));

            Model.Tenant development = repository.FindTenantByDomainIdentifier("tenant.example", "Development");
            Model.Tenant production = repository.FindTenantByDomainIdentifier("tenant.example", "Production");

            Assert.Equal("development", development.Name);
            Assert.Equal("production", production.Name);
            Assert.NotEqual(development.Id, production.Id);
            Assert.Equal(EntityState.Detached, context.Entry(development).State);
            Assert.Same(development, repository.FindTenantByDomainIdentifier("tenant.example", "Development"));
            Assert.Null(repository.FindTenantByDomainIdentifier("missing.example", "Development"));
        }

        /// <summary>
        /// A domain/environment pair cannot belong to multiple tenants.
        /// </summary>
        [Fact]
        public void DuplicateDomainOwnershipIsRejected()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            using var context = new InfrastructureDbContext(new DbContextOptionsBuilder<InfrastructureDbContext>().UseSqlite(connection).Options);
            context.Database.EnsureCreated();
            context.Tenants.AddRange(CreateTenant("first", "Development"), CreateTenant("second", "Development"));

            Assert.Throws<DbUpdateException>(() => context.SaveChanges());
            Assert.Equal(253, context.Model.FindEntityType(typeof(DomainIdentifier)).FindProperty(nameof(DomainIdentifier.DomainName)).GetMaxLength());
            Assert.Equal(64, context.Model.FindEntityType(typeof(DomainIdentifier)).FindProperty(nameof(DomainIdentifier.Environment)).GetMaxLength());
        }

        /// <summary>
        /// The SQL migration validates existing mappings before altering their schema.
        /// </summary>
        [Fact]
        public void DomainMigrationGuardsExistingData()
        {
            using InfrastructureDbContext context = new InfrastructureDbContext.DesignTimeFactory().CreateDbContext(Array.Empty<string>());
            string sql = context.GetService<IMigrator>().GenerateScript("20221220113759_IsPayPalEnabled");

            Assert.Contains("THROW 51000", sql);
            Assert.Contains("THROW 51001", sql);
            Assert.True(sql.IndexOf("THROW 51000", StringComparison.Ordinal) < sql.IndexOf("ALTER TABLE [DomainIdentifiers]", StringComparison.Ordinal));
            Assert.Contains("CREATE UNIQUE INDEX [IX_DomainIdentifiers_DomainName_Environment]", sql);
            Assert.DoesNotContain("ALTER TABLE [AspNetUser", sql);
            Assert.DoesNotContain("sp_rename", sql);
        }

        private static Model.Tenant CreateTenant(string name, string environment)
        {
            return new Model.Tenant
            {
                Name = name,
                PublicId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Domains = new List<DomainIdentifier>
                {
                    new DomainIdentifier { DomainName = "tenant.example", Environment = environment, Created = DateTime.UtcNow },
                },
            };
        }
    }
}