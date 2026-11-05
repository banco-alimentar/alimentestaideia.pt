// -----------------------------------------------------------------------
// <copyright file="TargetDatabaseService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Reads the target identity and the hardening migration's preconditions.
    /// </summary>
    public sealed class TargetDatabaseService
    {
        private const string ReadinessSql = @"
SELECT
  (SELECT COUNT_BIG(*) FROM (SELECT EasyPayPaymentId FROM dbo.Payments WHERE EasyPayPaymentId IS NOT NULL
     GROUP BY EasyPayPaymentId HAVING COUNT_BIG(*) > 1) duplicates),
  (SELECT COALESCE(SUM(duplicates.Total), 0) FROM (SELECT COUNT_BIG(*) AS Total FROM dbo.Payments WHERE EasyPayPaymentId IS NOT NULL
     GROUP BY EasyPayPaymentId HAVING COUNT_BIG(*) > 1) duplicates),
  (SELECT COUNT_BIG(*) FROM dbo.Payments WHERE LEN(EasyPayPaymentId) > 128),
  (SELECT COUNT_BIG(*) FROM (SELECT [Year], COALESCE(FoodBankId, 0) AS Scope, [Sequence]
     FROM dbo.Invoices GROUP BY [Year], COALESCE(FoodBankId, 0), [Sequence] HAVING COUNT_BIG(*) > 1) conflicts),
  CONVERT(bit, CASE WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL AND EXISTS
     (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @Harden) THEN 1 ELSE 0 END);";

        private readonly IDbContextFactory<ApplicationDbContext> factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetDatabaseService"/> class.
        /// </summary>
        /// <param name="factory">Context factory.</param>
        public TargetDatabaseService(IDbContextFactory<ApplicationDbContext> factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Reads the target identity.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The identity.</returns>
        public static async Task<TargetIdentity> GetIdentityAsync(ApplicationDbContext db, CancellationToken cancellationToken)
        {
            return (await SqlRunner.QueryAsync(
                db.Database,
                TargetIdentity.Sql,
                reader => new TargetIdentity(
                    SqlRunner.Get<string>(reader, 0) ?? string.Empty,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)),
                cancellationToken)).Single();
        }

        /// <summary>
        /// Reads the target identity with a new context.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The identity.</returns>
        public async Task<TargetIdentity> GetIdentityAsync(CancellationToken cancellationToken)
        {
            await using ApplicationDbContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            return await GetIdentityAsync(db, cancellationToken);
        }

        /// <summary>
        /// Reads identity, duplicate counts, migration state, and audit tables.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The readiness snapshot.</returns>
        public async Task<DatabaseReadiness> GetReadinessAsync(CancellationToken cancellationToken)
        {
            await using ApplicationDbContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            TargetIdentity identity = await GetIdentityAsync(db, cancellationToken);
            var counts = (await SqlRunner.QueryAsync(
                db.Database,
                ReadinessSql,
                reader => new
                {
                    Groups = reader.GetInt64(0),
                    Rows = reader.GetInt64(1),
                    Overlength = reader.GetInt64(2),
                    InvoiceConflicts = reader.GetInt64(3),
                    Applied = reader.GetBoolean(4),
                },
                cancellationToken,
                SqlRunner.Text("@Harden", DuplicateGroupLoader.HardenMigrationId))).Single();

            List<string> migrations = await SqlRunner.QueryAsync(
                db.Database,
                "IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL SELECT TOP (5) MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId DESC;",
                reader => reader.GetString(0),
                cancellationToken);

            AuditTablePresence audits = await DuplicateGroupLoader.LoadAuditTablePresenceAsync(db, cancellationToken);
            var auditCounts = new Dictionary<string, long>();
            foreach ((bool exists, string table) in new[]
            {
                (audits.Plans, "EasyPayPaymentIdReconciliationPlans"),
                (audits.Reconciliation, "EasyPayPaymentIdReconciliationAudit"),
                (audits.Retirement, "EasyPayPaymentIdRetirementAudit"),
                (audits.Deletion, "EasyPayPaymentDeletionAudit"),
            })
            {
                if (exists)
                {
                    auditCounts[table] = (long)(await SqlRunner.ScalarAsync(db.Database, $"SELECT COUNT_BIG(*) FROM dbo.{SqlRunner.QuoteName(table)};", cancellationToken))!;
                }
            }

            return new DatabaseReadiness
            {
                ServerName = identity.ServerName,
                DatabaseName = identity.DatabaseName,
                Login = identity.Login,
                ProductVersion = identity.ProductVersion,
                DuplicateGroups = counts.Groups,
                DuplicateRows = counts.Rows,
                OverlengthProviderIds = counts.Overlength,
                ConflictingInvoiceGroups = counts.InvoiceConflicts,
                HardenMigrationApplied = counts.Applied,
                RecentMigrations = migrations,
                AuditTables = audits,
                AuditRowCounts = auditCounts,
            };
        }
    }
}
