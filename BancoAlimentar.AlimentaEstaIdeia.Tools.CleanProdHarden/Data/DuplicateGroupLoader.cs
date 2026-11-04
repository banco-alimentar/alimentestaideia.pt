// -----------------------------------------------------------------------
// <copyright file="DuplicateGroupLoader.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Loads duplicate groups and their execution environment. Every query projects only columns
    /// that exist in production, because the model is ahead of the database for some tables.
    /// </summary>
    internal static class DuplicateGroupLoader
    {
        /// <summary>
        /// Migration that requires unique provider IDs.
        /// </summary>
        public const string HardenMigrationId = "20261024174319_HardenPaymentConcurrency";

        /// <summary>
        /// Evidence hash batch, identical in content to the SQL script's <c>ExpectedEvidenceHash</c>.
        /// </summary>
        private const string EvidenceHashSql = @"
SET NOCOUNT ON;
DECLARE @Members TABLE (PaymentId int NOT NULL PRIMARY KEY, DonationId int NULL, GroupId int NOT NULL, DonationConfirmedPaymentId int NULL);
INSERT @Members (PaymentId, DonationId, GroupId)
SELECT ranked.Id, ranked.DonationId, ranked.GroupId
FROM (
  SELECT payment.Id, payment.DonationId,
         MIN(payment.Id) OVER (PARTITION BY payment.EasyPayPaymentId) AS GroupId,
         COUNT(*) OVER (PARTITION BY payment.EasyPayPaymentId) AS PaymentCount
  FROM dbo.Payments payment
  WHERE payment.EasyPayPaymentId IS NOT NULL) ranked
WHERE ranked.PaymentCount > 1 AND (@GroupId IS NULL OR ranked.GroupId = @GroupId);
UPDATE member SET DonationConfirmedPaymentId = donation.ConfirmedPaymentId
FROM @Members member JOIN dbo.Donations donation ON donation.Id = member.DonationId;
SELECT grouped.GroupId,
  CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), CONCAT(
    (SELECT payment.* FROM dbo.Payments payment
     JOIN @Members member ON member.PaymentId = payment.Id
     WHERE member.GroupId = grouped.GroupId ORDER BY payment.Id FOR JSON PATH, INCLUDE_NULL_VALUES),
    N'|',
    (SELECT donation.* FROM dbo.Donations donation
     WHERE EXISTS (SELECT 1 FROM @Members member WHERE member.GroupId = grouped.GroupId
                   AND (member.PaymentId = donation.ConfirmedPaymentId OR member.DonationId = donation.Id))
     ORDER BY donation.Id FOR JSON PATH, INCLUDE_NULL_VALUES),
    N'|',
    (SELECT replacement.* FROM dbo.Payments replacement
     WHERE EXISTS (SELECT 1 FROM @Members member WHERE member.GroupId = grouped.GroupId
                   AND member.DonationConfirmedPaymentId = replacement.Id)
     ORDER BY replacement.Id FOR JSON PATH, INCLUDE_NULL_VALUES)
  ))), 2) AS EvidenceHash
FROM (SELECT DISTINCT GroupId FROM @Members) grouped;";

        private const string EnvironmentSql = @"
SELECT
  CONVERT(int, COALESCE(HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'VIEW DEFINITION'), 0)),
  (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = OBJECT_ID(N'dbo.Payments') AND is_disabled = 0),
  (SELECT COUNT_BIG(*) FROM dbo.Payments WHERE LEN(EasyPayPaymentId) > 128),
  (SELECT COUNT_BIG(*) FROM (SELECT [Year], COALESCE(FoodBankId, 0) AS Scope, [Sequence]
     FROM dbo.Invoices GROUP BY [Year], COALESCE(FoodBankId, 0), [Sequence] HAVING COUNT_BIG(*) > 1) conflicts);";

        private const string ForeignKeySql = @"
SELECT foreignKey.name, OBJECT_SCHEMA_NAME(foreignKey.parent_object_id), OBJECT_NAME(foreignKey.parent_object_id),
       parentColumn.name, referencedColumn.name, foreignKey.delete_referential_action_desc,
       (SELECT COUNT(*) FROM sys.foreign_key_columns component WHERE component.constraint_object_id = foreignKey.object_id)
FROM sys.foreign_keys foreignKey
JOIN sys.foreign_key_columns keyColumn ON keyColumn.constraint_object_id = foreignKey.object_id
JOIN sys.columns parentColumn ON parentColumn.object_id = keyColumn.parent_object_id AND parentColumn.column_id = keyColumn.parent_column_id
JOIN sys.columns referencedColumn ON referencedColumn.object_id = keyColumn.referenced_object_id AND referencedColumn.column_id = keyColumn.referenced_column_id
WHERE foreignKey.referenced_object_id = OBJECT_ID(N'dbo.Payments')
ORDER BY foreignKey.name, keyColumn.constraint_column_id;";

        /// <summary>
        /// Loads and evaluates duplicate groups.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="groupId">Optional group filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Evaluated groups ordered by group ID.</returns>
        public static async Task<IReadOnlyList<GroupEvaluation>> LoadEvaluationsAsync(ApplicationDbContext db, int? groupId, CancellationToken cancellationToken)
        {
            List<string> keys;
            if (groupId.HasValue)
            {
                string? key = await db.Payments.OfType<EasyPayBaseClass>()
                    .Where(payment => payment.Id == groupId.Value)
                    .Select(payment => payment.EasyPayPaymentId)
                    .SingleOrDefaultAsync(cancellationToken);
                keys = key == null ? new List<string>() : new List<string> { key };
            }
            else
            {
                keys = await db.Payments.OfType<EasyPayBaseClass>()
                    .Where(payment => payment.EasyPayPaymentId != null)
                    .GroupBy(payment => payment.EasyPayPaymentId)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToListAsync(cancellationToken);
            }

            if (keys.Count == 0)
            {
                return Array.Empty<GroupEvaluation>();
            }

            // EasyPayWithValuesBaseClass is not an EF entity type; each concrete type maps the shared Requested and Paid columns.
            var rows = await db.Payments.OfType<EasyPayBaseClass>()
                .Where(payment => keys.Contains(payment.EasyPayPaymentId))
                .Select(payment => new
                {
                    payment.Id,
                    payment.EasyPayPaymentId,
                    PaymentType = EF.Property<string>(payment, "Discriminator"),
                    payment.Created,
                    payment.Completed,
                    payment.Status,
                    payment.TransactionKey,
                    Requested = payment is CreditCardPayment ? (float?)((CreditCardPayment)payment).Requested
                        : payment is MBWayPayment ? (float?)((MBWayPayment)payment).Requested
                        : payment is MultiBankPayment ? (float?)((MultiBankPayment)payment).Requested
                        : null,
                    Paid = payment is CreditCardPayment ? (float?)((CreditCardPayment)payment).Paid
                        : payment is MBWayPayment ? (float?)((MBWayPayment)payment).Paid
                        : payment is MultiBankPayment ? (float?)((MultiBankPayment)payment).Paid
                        : null,
                    DonationId = EF.Property<int?>(payment, "DonationId"),
                })
                .ToListAsync(cancellationToken);

            List<int?> memberIds = rows.Select(row => (int?)row.Id).ToList();
            List<int> forwardIds = rows.Where(row => row.DonationId.HasValue).Select(row => row.DonationId!.Value).Distinct().ToList();
            List<DonationFacts> donations = await db.Donations
                .Where(donation => forwardIds.Contains(donation.Id) || memberIds.Contains(EF.Property<int?>(donation, "ConfirmedPaymentId")))
                .Select(donation => new DonationFacts(donation.Id, donation.PaymentStatus, donation.DonationAmount, EF.Property<int?>(donation, "ConfirmedPaymentId")))
                .ToListAsync(cancellationToken);
            Dictionary<int, DonationFacts> donationsById = donations.ToDictionary(donation => donation.Id);
            ILookup<int, DonationFacts> confirmationsByPayment = donations
                .Where(donation => donation.ConfirmedPaymentId.HasValue)
                .ToLookup(donation => donation.ConfirmedPaymentId!.Value);

            List<int> replacementIds = forwardIds
                .Where(donationsById.ContainsKey)
                .Select(id => donationsById[id].ConfirmedPaymentId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            Dictionary<int, ReplacementPaymentFacts> replacements = await LoadReplacementsAsync(db, replacementIds, cancellationToken);

            List<PaymentMemberInput> inputs = rows.Select(row =>
            {
                DonationFacts? forward = row.DonationId.HasValue && donationsById.TryGetValue(row.DonationId.Value, out DonationFacts? found) ? found : null;
                ReplacementPaymentFacts? replacement = forward?.ConfirmedPaymentId is int replacementId && replacements.TryGetValue(replacementId, out ReplacementPaymentFacts? payment)
                    ? payment
                    : null;
                return new PaymentMemberInput
                {
                    PaymentId = row.Id,
                    EasyPayPaymentId = row.EasyPayPaymentId,
                    PaymentType = row.PaymentType,
                    Created = row.Created,
                    Completed = row.Completed,
                    Status = row.Status,
                    TransactionKey = row.TransactionKey,
                    Requested = row.Requested,
                    Paid = row.Paid,
                    DonationId = row.DonationId,
                    ForwardDonation = forward,
                    ConfirmingDonations = confirmationsByPayment[row.Id].OrderBy(donation => donation.Id).ToList(),
                    Replacement = replacement,
                };
            }).ToList();

            return DuplicateGroupRules.EvaluateGroups(inputs);
        }

        /// <summary>
        /// Computes evidence hashes with the SQL script's exact formula.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="groupId">Optional group filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Hashes by group ID.</returns>
        public static async Task<IReadOnlyDictionary<int, string>> LoadEvidenceHashesAsync(ApplicationDbContext db, int? groupId, CancellationToken cancellationToken)
        {
            List<(int GroupId, string Hash)> rows = await SqlRunner.QueryAsync(
                db.Database,
                EvidenceHashSql,
                reader => (reader.GetInt32(0), reader.GetString(1)),
                cancellationToken,
                SqlRunner.Int("@GroupId", groupId));
            return rows.ToDictionary(row => row.GroupId, row => row.Hash);
        }

        /// <summary>
        /// Loads invoice and subscription evidence used by recommendations.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="groups">Groups.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Donations with invoices and subscription IDs by donation.</returns>
        public static async Task<(HashSet<int> Invoiced, Dictionary<int, int> Subscriptions)> LoadDonationContextAsync(
            ApplicationDbContext db,
            IEnumerable<GroupEvaluation> groups,
            CancellationToken cancellationToken)
        {
            List<int?> donationIds = groups
                .SelectMany(group => group.Members)
                .SelectMany(member => member.Input.ConfirmingDonations.Select(donation => donation.Id).Append(member.Input.DonationId ?? 0))
                .Where(id => id > 0)
                .Distinct()
                .Select(id => (int?)id)
                .ToList();
            if (donationIds.Count == 0)
            {
                return (new HashSet<int>(), new Dictionary<int, int>());
            }

            List<int?> invoiced = await db.Invoices
                .Where(invoice => donationIds.Contains(EF.Property<int?>(invoice, "DonationId")))
                .Select(invoice => EF.Property<int?>(invoice, "DonationId"))
                .Distinct()
                .ToListAsync(cancellationToken);

            var links = await db.SubscriptionDonations
                .Where(link => donationIds.Contains(link.DonationId) && EF.Property<int?>(link, "SubscriptionId") != null)
                .Select(link => new { link.DonationId, SubscriptionId = EF.Property<int?>(link, "SubscriptionId") })
                .ToListAsync(cancellationToken);
            var initial = await db.Subscriptions
                .Where(subscription => donationIds.Contains(EF.Property<int?>(subscription, "InitialDonationId")))
                .Select(subscription => new { DonationId = EF.Property<int?>(subscription, "InitialDonationId"), SubscriptionId = (int?)subscription.Id })
                .ToListAsync(cancellationToken);

            var subscriptions = new Dictionary<int, int>();
            foreach (var link in links.Concat(initial).Where(link => link.DonationId.HasValue && link.SubscriptionId.HasValue))
            {
                subscriptions.TryAdd(link.DonationId!.Value, link.SubscriptionId!.Value);
            }

            return (invoiced.Where(id => id.HasValue).Select(id => id!.Value).ToHashSet(), subscriptions);
        }

        /// <summary>
        /// Loads the database facts that validation needs for a set of payments.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="paymentIds">Payments to check for dependents and audit history.</param>
        /// <param name="evidenceHash">Current evidence hash for single-group validation.</param>
        /// <param name="lockDependencies">Take table locks on referencing tables, as the SQL script does.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The environment.</returns>
        public static async Task<ExecutionEnvironment> LoadEnvironmentAsync(
            ApplicationDbContext db,
            IReadOnlyCollection<int> paymentIds,
            string evidenceHash,
            bool lockDependencies,
            CancellationToken cancellationToken)
        {
            var facts = (await SqlRunner.QueryAsync(
                db.Database,
                EnvironmentSql,
                reader => new
                {
                    ViewDefinition = reader.GetInt32(0) == 1,
                    Triggers = reader.GetInt32(1),
                    Overlength = reader.GetInt64(2),
                    InvoiceConflicts = reader.GetInt64(3),
                },
                cancellationToken)).Single();

            List<ForeignKeyInfo> foreignKeys = await LoadForeignKeysAsync(db, cancellationToken);
            List<string> unsupported = foreignKeys.Where(key => !key.IsSupported).Select(key => key.Describe()).ToList();
            List<PaymentDependency> dependencies = await LoadDependenciesAsync(db, foreignKeys.Where(key => key.IsSupported).ToList(), paymentIds, lockDependencies, cancellationToken);
            AuditIds audits = await LoadAuditIdsAsync(db, paymentIds, cancellationToken);

            return new ExecutionEnvironment
            {
                CurrentEvidenceHash = evidenceHash,
                HasViewDefinitionPermission = facts.ViewDefinition,
                UnsupportedForeignKeys = unsupported,
                EnabledPaymentTriggerCount = facts.Triggers,
                Dependencies = dependencies,
                OverlengthProviderIds = facts.Overlength,
                ConflictingInvoiceGroups = facts.InvoiceConflicts,
                ReconciliationAuditPaymentIds = audits.Reconciliation,
                RetirementAuditPaymentIds = audits.Retirement,
                DeletionAuditPaymentIds = audits.Deletion,
            };
        }

        /// <summary>
        /// Reads which audit tables exist.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Existence flags.</returns>
        public static async Task<AuditTablePresence> LoadAuditTablePresenceAsync(ApplicationDbContext db, CancellationToken cancellationToken)
        {
            const string sql = @"
SELECT CONVERT(bit, CASE WHEN OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationAudit', N'U') IS NULL THEN 0 ELSE 1 END),
       CONVERT(bit, CASE WHEN OBJECT_ID(N'dbo.EasyPayPaymentIdReconciliationPlans', N'U') IS NULL THEN 0 ELSE 1 END),
       CONVERT(bit, CASE WHEN OBJECT_ID(N'dbo.EasyPayPaymentIdRetirementAudit', N'U') IS NULL THEN 0 ELSE 1 END),
       CONVERT(bit, CASE WHEN OBJECT_ID(N'dbo.EasyPayPaymentDeletionAudit', N'U') IS NULL THEN 0 ELSE 1 END);";
            return (await SqlRunner.QueryAsync(
                db.Database,
                sql,
                reader => new AuditTablePresence(reader.GetBoolean(0), reader.GetBoolean(1), reader.GetBoolean(2), reader.GetBoolean(3)),
                cancellationToken)).Single();
        }

        private static async Task<Dictionary<int, ReplacementPaymentFacts>> LoadReplacementsAsync(ApplicationDbContext db, List<int> ids, CancellationToken cancellationToken)
        {
            if (ids.Count == 0)
            {
                return new Dictionary<int, ReplacementPaymentFacts>();
            }

            List<ReplacementPaymentFacts> replacements = await db.Payments
                .Where(payment => ids.Contains(payment.Id))
                .Select(payment => new ReplacementPaymentFacts(
                    payment.Id,
                    EF.Property<string>(payment, "Discriminator"),
                    payment.Status,
                    payment is CreditCardPayment ? (float?)((CreditCardPayment)payment).Paid : payment is MBWayPayment ? (float?)((MBWayPayment)payment).Paid : payment is MultiBankPayment ? (float?)((MultiBankPayment)payment).Paid : null,
                    EF.Property<int?>(payment, "DonationId"),
                    payment is PayPalPayment ? ((PayPalPayment)payment).PayPalPaymentId : null,
                    payment is PayPalPayment ? ((PayPalPayment)payment).PayerId : null))
                .ToListAsync(cancellationToken);
            return replacements.ToDictionary(payment => payment.Id);
        }

        private static async Task<List<ForeignKeyInfo>> LoadForeignKeysAsync(ApplicationDbContext db, CancellationToken cancellationToken)
        {
            var columns = await SqlRunner.QueryAsync(
                db.Database,
                ForeignKeySql,
                reader => new
                {
                    Name = reader.GetString(0),
                    Schema = reader.GetString(1),
                    Table = reader.GetString(2),
                    Column = reader.GetString(3),
                    ReferencedColumn = reader.GetString(4),
                    DeleteAction = reader.GetString(5),
                    ColumnCount = reader.GetInt32(6),
                },
                cancellationToken);
            return columns
                .GroupBy(column => column.Name)
                .Select(key =>
                {
                    var first = key.First();
                    return new ForeignKeyInfo(
                        key.Key,
                        first.Schema,
                        first.Table,
                        first.Column,
                        first.DeleteAction,
                        first.ColumnCount == 1 && string.Equals(first.ReferencedColumn, "Id", StringComparison.Ordinal));
                })
                .ToList();
        }

        private static async Task<List<PaymentDependency>> LoadDependenciesAsync(
            ApplicationDbContext db,
            List<ForeignKeyInfo> foreignKeys,
            IReadOnlyCollection<int> paymentIds,
            bool lockDependencies,
            CancellationToken cancellationToken)
        {
            if (foreignKeys.Count == 0 || paymentIds.Count == 0)
            {
                return new List<PaymentDependency>();
            }

            string hint = lockDependencies ? " WITH (TABLOCK, HOLDLOCK)" : string.Empty;
            var sql = new StringBuilder();
            for (int index = 0; index < foreignKeys.Count; index++)
            {
                ForeignKeyInfo key = foreignKeys[index];
                string column = "dependency." + SqlRunner.QuoteName(key.Column);
                if (index > 0)
                {
                    sql.AppendLine("UNION ALL");
                }

                sql.Append(CultureInfo.InvariantCulture, $"SELECT {index} AS KeyIndex, {column} AS PaymentId, COUNT_BIG(*) AS ReferenceCount ")
                    .Append(CultureInfo.InvariantCulture, $"FROM {SqlRunner.QuoteName(key.Schema)}.{SqlRunner.QuoteName(key.Table)} dependency{hint} ")
                    .Append(CultureInfo.InvariantCulture, $"WHERE {column} IN (SELECT CONVERT(int, [value]) FROM OPENJSON(@ids)) GROUP BY {column}")
                    .AppendLine();
            }

            var rows = await SqlRunner.QueryAsync(
                db.Database,
                sql.ToString(),
                reader => new { KeyIndex = reader.GetInt32(0), PaymentId = reader.GetInt32(1), Count = reader.GetInt64(2) },
                cancellationToken,
                SqlRunner.Ids("@ids", paymentIds));
            return rows
                .Select(row =>
                {
                    ForeignKeyInfo key = foreignKeys[row.KeyIndex];
                    return new PaymentDependency(row.PaymentId, key.Schema + "." + key.Table, key.Column, key.DeleteAction, row.Count);
                })
                .OrderBy(dependency => dependency.PaymentId)
                .ThenBy(dependency => dependency.ReferencingTable, StringComparer.Ordinal)
                .ToList();
        }

        private static async Task<AuditIds> LoadAuditIdsAsync(ApplicationDbContext db, IReadOnlyCollection<int> paymentIds, CancellationToken cancellationToken)
        {
            var result = new AuditIds();
            if (paymentIds.Count == 0)
            {
                return result;
            }

            AuditTablePresence presence = await LoadAuditTablePresenceAsync(db, cancellationToken);
            var parts = new List<string>();
            if (presence.Reconciliation)
            {
                parts.Add("SELECT 0, PaymentId FROM dbo.EasyPayPaymentIdReconciliationAudit WHERE PaymentId IN (SELECT CONVERT(int, [value]) FROM OPENJSON(@ids))");
            }

            if (presence.Retirement)
            {
                parts.Add("SELECT 1, PaymentId FROM dbo.EasyPayPaymentIdRetirementAudit WHERE PaymentId IN (SELECT CONVERT(int, [value]) FROM OPENJSON(@ids))");
            }

            if (presence.Deletion)
            {
                parts.Add("SELECT 2, PaymentId FROM dbo.EasyPayPaymentDeletionAudit WHERE PaymentId IN (SELECT CONVERT(int, [value]) FROM OPENJSON(@ids))");
            }

            if (parts.Count == 0)
            {
                return result;
            }

            List<(int Kind, int PaymentId)> rows = await SqlRunner.QueryAsync(
                db.Database,
                string.Join(Environment.NewLine + "UNION ALL" + Environment.NewLine, parts) + ";",
                reader => (reader.GetInt32(0), reader.GetInt32(1)),
                cancellationToken,
                SqlRunner.Ids("@ids", paymentIds));
            foreach ((int kind, int paymentId) in rows)
            {
                (kind == 0 ? result.Reconciliation : kind == 1 ? result.Retirement : result.Deletion).Add(paymentId);
            }

            return result;
        }

        private sealed record ForeignKeyInfo(string Name, string Schema, string Table, string Column, string DeleteAction, bool IsSupported)
        {
            public string Describe() => $"{this.Name} ({this.Schema}.{this.Table}.{this.Column})";
        }

        private sealed class AuditIds
        {
            public HashSet<int> Reconciliation { get; } = new HashSet<int>();

            public HashSet<int> Retirement { get; } = new HashSet<int>();

            public HashSet<int> Deletion { get; } = new HashSet<int>();
        }
    }
}
