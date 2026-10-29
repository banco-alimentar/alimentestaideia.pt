// -----------------------------------------------------------------------
// <copyright file="SqlServerFiscalMigrationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Exercises the fiscal schema against SQL Server instead of the in-memory provider.
    /// Set RUN_SQLSERVER_FISCAL_TESTS=1 on a Docker-capable host to run this collection.
    /// </summary>
    [Collection("SQL Server fiscal")]
    public class SqlServerFiscalMigrationTests
    {
        private const string FiscalFoundationMigration = "20260918235534_FiscalDocumentFoundation";

        private readonly SqlServerFiscalFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerFiscalMigrationTests"/> class.
        /// </summary>
        /// <param name="fixture">The SQL Server container fixture.</param>
        public SqlServerFiscalMigrationTests(SqlServerFiscalFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Fiscal migrations must not alter historical operational receipts.
        /// </summary>
        /// <returns>An asynchronous test operation.</returns>
        [SkippableFact]
        public async Task MigratingFiscalSchema_PreservesLegacyInvoiceRows()
        {
            this.fixture.SkipUnlessAvailable();
            await using SqlServerFiscalDatabase database = await this.fixture.CreateDatabaseAsync();

            await database.MigrateBeforeAsync(FiscalFoundationMigration);
            int invoiceId = await database.InsertLegacyInvoiceAsync();

            await database.MigrateToLatestAsync();
            await using ApplicationDbContext context = database.CreateContext();
            Invoice invoice = await context.Invoices.SingleAsync(item => item.Id == invoiceId);

            Assert.Equal("LEGACY-2026-1", invoice.Number);
            Assert.Equal(1, invoice.Sequence);
            Assert.Empty(await context.FiscalDocuments.ToListAsync());
        }

        /// <summary>
        /// Fiscal migrations create SQL Server constraints rather than relying only on model
        /// validation by the application process.
        /// </summary>
        /// <returns>An asynchronous test operation.</returns>
        [SkippableFact]
        public async Task MigratingFiscalSchema_CreatesRequiredSqlIndexesForeignKeysAndChecks()
        {
            this.fixture.SkipUnlessAvailable();
            await using SqlServerFiscalDatabase database = await this.fixture.CreateDatabaseAsync();
            await database.MigrateToLatestAsync();

            string indexes = await database.ReadRowsAsync(
                "SELECT [name] + N':' + ISNULL([filter_definition], N'') " +
                "FROM sys.indexes " +
                "WHERE [object_id] = OBJECT_ID(N'[dbo].[FiscalDocuments]') " +
                "AND [name] IN (N'IX_FiscalDocuments_IdempotencyKey', N'IX_FiscalDocuments_DonationId', " +
                "N'IX_FiscalDocuments_OriginalFiscalDocumentId', N'IX_FiscalDocuments_SeriesId_Sequence') " +
                "ORDER BY [name]");
            string foreignKeys = await database.ReadRowsAsync(
                "SELECT [name] FROM sys.foreign_keys " +
                "WHERE [parent_object_id] = OBJECT_ID(N'[dbo].[FiscalDocuments]') ORDER BY [name]");
            string checks = await database.ReadRowsAsync(
                "SELECT [name] FROM sys.check_constraints " +
                "WHERE [parent_object_id] = OBJECT_ID(N'[dbo].[FiscalDocuments]') ORDER BY [name]");

            Assert.Contains("IX_FiscalDocuments_IdempotencyKey", indexes, StringComparison.Ordinal);
            Assert.Contains("IdempotencyKey] IS NOT NULL", indexes, StringComparison.Ordinal);
            Assert.Contains("IX_FiscalDocuments_DonationId", indexes, StringComparison.Ordinal);
            Assert.Contains("[Kind] = 0", indexes, StringComparison.Ordinal);
            Assert.Contains("IX_FiscalDocuments_OriginalFiscalDocumentId", indexes, StringComparison.Ordinal);
            Assert.Contains("[Kind] = 1", indexes, StringComparison.Ordinal);
            Assert.Contains("IX_FiscalDocuments_SeriesId_Sequence", indexes, StringComparison.Ordinal);
            Assert.Contains("FK_FiscalDocuments_Donations_DonationId", foreignKeys, StringComparison.Ordinal);
            Assert.Contains("FK_FiscalDocuments_FiscalDocumentSeries_SeriesId_FiscalIssuerProfileId", foreignKeys, StringComparison.Ordinal);
            Assert.Contains("FK_FiscalDocuments_FiscalDocuments_OriginalFiscalDocumentId", foreignKeys, StringComparison.Ordinal);
            Assert.Contains("CK_FiscalDocuments_Kind_OriginalFiscalDocumentId", checks, StringComparison.Ordinal);
            Assert.Contains("CK_FiscalDocuments_InvoicingStrategy_FoodBankId", checks, StringComparison.Ordinal);
        }

        /// <summary>
        /// Concurrent reservations in independent SQL Server contexts allocate different series
        /// numbers and ATCUD values.
        /// </summary>
        /// <returns>An asynchronous test operation.</returns>
        [SkippableFact]
        public async Task ConcurrentReservations_AllocateDistinctSequencesAndAtcudValues()
        {
            this.fixture.SkipUnlessAvailable();
            await using SqlServerFiscalDatabase database = await this.fixture.CreateDatabaseAsync();
            await database.MigrateToLatestAsync();
            FiscalReservationSeed seed = await database.SeedReservationAsync();

            Task<FiscalDocument> firstReservation = Task.Run(
                () => ReserveOriginal(database, seed.FirstDonationId, seed.SeriesId, "sql-concurrent-first"));
            Task<FiscalDocument> secondReservation = Task.Run(
                () => ReserveOriginal(database, seed.SecondDonationId, seed.SeriesId, "sql-concurrent-second"));

            FiscalDocument[] reservations = await Task.WhenAll(firstReservation, secondReservation);
            await using ApplicationDbContext verificationContext = database.CreateContext();
            List<FiscalDocument> persisted = await verificationContext.FiscalDocuments
                .Where(item => item.SeriesId == seed.SeriesId)
                .OrderBy(item => item.Sequence)
                .ToListAsync();
            FiscalDocumentSeries series = await verificationContext.FiscalDocumentSeries
                .SingleAsync(item => item.Id == seed.SeriesId);

            Assert.Equal(2, reservations.Select(item => item.Id).Distinct().Count());
            Assert.Equal(new[] { 1, 2 }, persisted.Select(item => item.Sequence));
            Assert.Equal(new[] { "AA-RELATIONAL-1", "AA-RELATIONAL-2" }, persisted.Select(item => item.Atcud));
            Assert.Equal(3, series.NextSequence);
        }

        private static FiscalDocument ReserveOriginal(
            SqlServerFiscalDatabase database,
            int donationId,
            int seriesId,
            string idempotencyKey)
        {
            using ApplicationDbContext context = database.CreateContext();
            FiscalDocumentSeries series = context.FiscalDocumentSeries
                .Include(item => item.FiscalIssuerProfile)
                .Single(item => item.Id == seriesId);
            var httpContext = new DefaultHttpContext();
            httpContext.SetTenant(new Tenant
            {
                Id = 1,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
            });
            var accessor = new HttpContextAccessor
            {
                HttpContext = httpContext,
            };
            var service = new FiscalDocumentReservationService(context, accessor);

            return service.ReserveOriginal(new FiscalDocumentReservationRequest
            {
                DonationId = donationId,
                SeriesId = seriesId,
                IdempotencyKey = idempotencyKey,
                FiscalScope = new FiscalScope
                {
                    TenantId = 1,
                    InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                    FiscalIssuerProfile = series.FiscalIssuerProfile,
                    FiscalDocumentSeries = series,
                },
            });
        }
    }
}
