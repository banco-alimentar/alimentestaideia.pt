// -----------------------------------------------------------------------
// <copyright file="SqlServerFiscalDatabase.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Represents one disposable SQL Server database used by a relational fiscal test.
    /// </summary>
    public sealed class SqlServerFiscalDatabase : IAsyncDisposable
    {
        private const string WebMigrationsAssembly = "BancoAlimentar.AlimentaEstaIdeia.Web";
        private readonly string connectionString;
        private readonly string databaseName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerFiscalDatabase"/> class.
        /// </summary>
        /// <param name="connectionString">The isolated database connection string.</param>
        /// <param name="databaseName">The isolated database name.</param>
        public SqlServerFiscalDatabase(string connectionString, string databaseName)
        {
            this.connectionString = connectionString;
            this.databaseName = databaseName;
        }

        /// <summary>
        /// Creates an application context configured to use the web migration assembly.
        /// </summary>
        /// <returns>The configured context.</returns>
        public ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(this.connectionString, sqlServer => sqlServer.MigrationsAssembly(WebMigrationsAssembly))
                .Options;
            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Migrates the database to the migration immediately preceding <paramref name="migration"/>.
        /// </summary>
        /// <param name="migration">The migration that must not yet be applied.</param>
        /// <returns>An asynchronous test operation.</returns>
        public async Task MigrateBeforeAsync(string migration)
        {
            await using ApplicationDbContext context = this.CreateContext();
            List<string> migrations = context.Database.GetMigrations().ToList();
            int migrationIndex = migrations.IndexOf(migration);
            Assert.True(migrationIndex > 0, $"Migration {migration} must have a predecessor.");
            await context.Database.MigrateAsync(migrations[migrationIndex - 1]);
        }

        /// <summary>
        /// Migrates the database to the latest application schema.
        /// </summary>
        /// <returns>An asynchronous test operation.</returns>
        public async Task MigrateToLatestAsync()
        {
            await using ApplicationDbContext context = this.CreateContext();
            await context.Database.MigrateAsync();
        }

        /// <summary>
        /// Inserts a legacy operational receipt before fiscal schema migration.
        /// </summary>
        /// <returns>The legacy invoice identifier.</returns>
        public async Task<int> InsertLegacyInvoiceAsync()
        {
            await using ApplicationDbContext context = this.CreateContext();
            var donation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationDate = DateTime.UtcNow,
                DonationAmount = 10,
                WantsReceipt = true,
            };
            var invoice = new Invoice
            {
                Sequence = 1,
                Number = "LEGACY-2026-1",
                Created = DateTime.UtcNow,
                Year = 2026,
                BlobName = Guid.NewGuid(),
                Donation = donation,
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            return invoice.Id;
        }

        /// <summary>
        /// Seeds one series and two donations for concurrent reservation verification.
        /// </summary>
        /// <returns>The persisted identifiers required by the test.</returns>
        public async Task<FiscalReservationSeed> SeedReservationAsync()
        {
            await using ApplicationDbContext context = this.CreateContext();
            var profile = new FiscalIssuerProfile
            {
                ScopeKind = FiscalScopeKind.Tenant,
                LegalName = "Emitente relacional",
                LegalAddress = "Rua de teste, 1, Lisboa",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "M00",
                SoftwareCertificateNumber = "0",
                SigningKeyIdentifier = "sql-test-key",
                SigningKeyVersion = "v1",
                IsActive = true,
                ValidFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            };
            var series = new FiscalDocumentSeries
            {
                FiscalIssuerProfile = profile,
                SeriesCode = "RC-RELATIONAL",
                SaftDocumentType = "RC",
                ProcessingMethod = FiscalProcessingMethod.CertifiedSoftware,
                FirstSequence = 1,
                NextSequence = 1,
                AtcudValidationCode = "AA-RELATIONAL",
                AtCommunicationReference = "sql-relational-test",
                Status = FiscalSeriesStatus.Active,
                StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            };
            var firstDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationDate = DateTime.UtcNow,
                DonationAmount = 10,
            };
            var secondDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationDate = DateTime.UtcNow,
                DonationAmount = 20,
            };
            context.AddRange(series, firstDonation, secondDonation);
            await context.SaveChangesAsync();

            return new FiscalReservationSeed(series.Id, firstDonation.Id, secondDonation.Id);
        }

        /// <summary>
        /// Reads a newline-delimited result set from the isolated database.
        /// </summary>
        /// <param name="query">The catalog query to execute.</param>
        /// <returns>The returned catalog rows.</returns>
        public async Task<string> ReadRowsAsync(string query)
        {
            await using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync();
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            await using DbDataReader reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                rows.Add(reader.GetString(0));
            }

            return string.Join(Environment.NewLine, rows);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.connectionString)
            {
                InitialCatalog = "master",
            };
            SqlConnection.ClearAllPools();
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await using DbCommand command = connection.CreateCommand();
            command.CommandText =
                $"IF DB_ID(N'{this.databaseName}') IS NOT NULL " +
                $"BEGIN ALTER DATABASE [{this.databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{this.databaseName}]; END";
            await command.ExecuteNonQueryAsync();
        }
    }
}
