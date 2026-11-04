// -----------------------------------------------------------------------
// <copyright file="GroupDetailLoader.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Loads everything the reviewer needs to see for one group. Raw rows come from <c>FOR JSON</c>
    /// so columns that are not in the model are shown too.
    /// </summary>
    internal static class GroupDetailLoader
    {
        private const string PaymentItemsSql = @"
IF OBJECT_ID(N'dbo.PaymentItems', N'U') IS NOT NULL
  SELECT PaymentId, DonationId FROM dbo.PaymentItems
  WHERE PaymentId IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids));";

        /// <summary>
        /// Loads the detail for an analyzed group.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="group">Analyzed group.</param>
        /// <param name="environment">Environment for the group.</param>
        /// <param name="audits">Which audit tables exist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detail.</returns>
        public static async Task<GroupDetail> LoadAsync(
            ApplicationDbContext db,
            AnalyzedGroup group,
            ExecutionEnvironment environment,
            AuditTablePresence audits,
            CancellationToken cancellationToken)
        {
            List<MemberEvaluation> members = group.Evaluation.Members.ToList();
            List<int> memberIds = members.Select(member => member.PaymentId).ToList();
            List<int> replacementIds = members
                .Select(member => member.Input.Replacement?.Id ?? member.DonationConfirmedPaymentId)
                .Where(id => id.HasValue && !memberIds.Contains(id.Value))
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var relations = new Dictionary<int, List<string>>();
            foreach (MemberEvaluation member in members)
            {
                if (member.Input.DonationId is int forward)
                {
                    AddRelation(relations, forward, $"Payment {member.PaymentId} points at it (Payments.DonationId).");
                }

                foreach (DonationFacts confirmed in member.Input.ConfirmingDonations)
                {
                    AddRelation(relations, confirmed.Id, $"It names payment {member.PaymentId} as its confirmed payment (Donations.ConfirmedPaymentId).");
                }
            }

            List<int> donationIds = relations.Keys.OrderBy(id => id).ToList();
            List<int> allPaymentIds = memberIds.Concat(replacementIds).ToList();

            Dictionary<int, IReadOnlyList<KeyValuePair<string, string?>>> paymentRows = await LoadRawRowsAsync(db, RawTable.Payments, allPaymentIds, cancellationToken);
            Dictionary<int, IReadOnlyList<KeyValuePair<string, string?>>> donationRows = await LoadRawRowsAsync(db, RawTable.Donations, donationIds, cancellationToken);
            List<NotificationInfo> notifications = await LoadNotificationsAsync(db, allPaymentIds, cancellationToken);
            List<EmailInfo> emails = await LoadEmailsAsync(db, allPaymentIds, cancellationToken);
            List<(int PaymentId, int? DonationId)> paymentItems = await SqlRunner.QueryAsync(
                db.Database,
                PaymentItemsSql,
                reader => (reader.GetInt32(0), SqlRunner.Get<int?>(reader, 1)),
                cancellationToken,
                SqlRunner.Ids("@Ids", allPaymentIds));

            PaymentDetail BuildPayment(int id) => new PaymentDetail(
                id,
                paymentRows.TryGetValue(id, out IReadOnlyList<KeyValuePair<string, string?>>? columns) ? columns : Array.Empty<KeyValuePair<string, string?>>(),
                notifications.Where(notification => notification.PaymentId == id).ToList(),
                emails.Where(email => email.PaymentId == id).ToList(),
                paymentItems.Where(item => item.PaymentId == id).Select(item => item.DonationId).ToList());

            return new GroupDetail
            {
                Group = group,
                Environment = environment,
                Members = memberIds.Select(BuildPayment).ToList(),
                Replacements = replacementIds.Select(BuildPayment).ToList(),
                Donations = await LoadDonationsAsync(db, donationIds, relations, donationRows, cancellationToken),
                AuditHistory = await LoadAuditHistoryAsync(db, memberIds, audits, cancellationToken),
            };
        }

        /// <summary>
        /// Reads rows as stored, one column per entry, in column order.
        /// </summary>
        /// <param name="db">Context.</param>
        /// <param name="table">Table to read.</param>
        /// <param name="ids">Row IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Columns by row ID.</returns>
        public static async Task<Dictionary<int, IReadOnlyList<KeyValuePair<string, string?>>>> LoadRawRowsAsync(
            ApplicationDbContext db,
            RawTable table,
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<int, IReadOnlyList<KeyValuePair<string, string?>>>();
            if (ids.Count == 0)
            {
                return result;
            }

            string tableName = table switch
            {
                RawTable.Payments => "dbo.Payments",
                RawTable.Donations => "dbo.Donations",
                _ => throw new ArgumentOutOfRangeException(nameof(table)),
            };
            string sql = $@"
SELECT (SELECT source.* FROM {tableName} source
        WHERE source.Id IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids))
        ORDER BY source.Id FOR JSON PATH, INCLUDE_NULL_VALUES);";
            string? json = (string?)await SqlRunner.ScalarAsync(db.Database, sql, cancellationToken, SqlRunner.Ids("@Ids", ids));
            if (string.IsNullOrEmpty(json))
            {
                return result;
            }

            using JsonDocument document = JsonDocument.Parse(json);
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                var columns = new List<KeyValuePair<string, string?>>();
                foreach (JsonProperty property in row.EnumerateObject())
                {
                    string? value = property.Value.ValueKind switch
                    {
                        JsonValueKind.Null => null,
                        JsonValueKind.String => property.Value.GetString(),
                        _ => property.Value.GetRawText(),
                    };
                    columns.Add(new KeyValuePair<string, string?>(property.Name, value));
                }

                result[row.GetProperty("Id").GetInt32()] = columns;
            }

            return result;
        }

        private static void AddRelation(Dictionary<int, List<string>> relations, int donationId, string text)
        {
            if (!relations.TryGetValue(donationId, out List<string>? list))
            {
                list = new List<string>();
                relations[donationId] = list;
            }

            if (!list.Contains(text))
            {
                list.Add(text);
            }
        }

        private static async Task<List<NotificationInfo>> LoadNotificationsAsync(ApplicationDbContext db, List<int> paymentIds, CancellationToken cancellationToken)
        {
            List<int?> ids = paymentIds.Select(id => (int?)id).ToList();
            var rows = await db.PaymentNotifications
                .Where(notification => ids.Contains(EF.Property<int?>(notification, "PaymentId")))
                .OrderBy(notification => notification.Created)
                .Select(notification => new
                {
                    notification.Id,
                    PaymentId = EF.Property<int?>(notification, "PaymentId"),
                    notification.Created,
                    notification.Subject,
                    notification.NotificationType,
                })
                .ToListAsync(cancellationToken);
            return rows
                .Select(row => new NotificationInfo(row.Id, row.PaymentId ?? 0, row.Created, row.Subject, row.NotificationType.ToString()))
                .ToList();
        }

        private static async Task<List<EmailInfo>> LoadEmailsAsync(ApplicationDbContext db, List<int> paymentIds, CancellationToken cancellationToken)
        {
            List<int?> ids = paymentIds.Select(id => (int?)id).ToList();
            var rows = await db.EmailCommunications
                .Where(email => ids.Contains(email.PaymentId))
                .OrderBy(email => email.SentAtUtc)
                .Select(email => new { email.Id, email.PaymentId, email.SentAtUtc, email.Subject, email.ToAddress })
                .ToListAsync(cancellationToken);
            return rows.Select(row => new EmailInfo(row.Id, row.PaymentId ?? 0, row.SentAtUtc, row.Subject, row.ToAddress)).ToList();
        }

        private static async Task<List<DonationDetail>> LoadDonationsAsync(
            ApplicationDbContext db,
            List<int> donationIds,
            Dictionary<int, List<string>> relations,
            Dictionary<int, IReadOnlyList<KeyValuePair<string, string?>>> rawRows,
            CancellationToken cancellationToken)
        {
            if (donationIds.Count == 0)
            {
                return new List<DonationDetail>();
            }

            List<int?> nullableIds = donationIds.Select(id => (int?)id).ToList();

            var donations = await db.Donations
                .Where(donation => donationIds.Contains(donation.Id))
                .Select(donation => new
                {
                    donation.Id,
                    ConfirmedPaymentId = EF.Property<int?>(donation, "ConfirmedPaymentId"),
                    FoodBankName = donation.FoodBank != null ? donation.FoodBank.Name : null,
                    Donor = donation.User == null ? null : new
                    {
                        donation.User.Id,
                        donation.User.FullName,
                        donation.User.Email,
                        donation.User.Nif,
                        donation.User.PhoneNumber,
                        donation.User.CompanyName,
                        donation.User.IsAnonymous,
                    },
                })
                .ToListAsync(cancellationToken);

            var items = await db.DonationItems
                .Where(item => donationIds.Contains(EF.Property<int>(item, "DonationId")))
                .OrderBy(item => item.Id)
                .Select(item => new
                {
                    DonationId = EF.Property<int>(item, "DonationId"),
                    item.Id,
                    ProductName = item.ProductCatalogue != null ? item.ProductCatalogue.Name : null,
                    item.Quantity,
                    item.Price,
                })
                .ToListAsync(cancellationToken);

            var invoices = await db.Invoices
                .Where(invoice => nullableIds.Contains(EF.Property<int?>(invoice, "DonationId")))
                .OrderBy(invoice => invoice.Id)
                .Select(invoice => new
                {
                    DonationId = EF.Property<int?>(invoice, "DonationId"),
                    Info = new InvoiceInfo(
                        invoice.Id,
                        invoice.Number,
                        invoice.Year,
                        invoice.Sequence,
                        invoice.Created,
                        invoice.IsCanceled,
                        invoice.FoodBank != null ? invoice.FoodBank.Name : null,
                        invoice.InternalMessage),
                })
                .ToListAsync(cancellationToken);

            var subscriptionLinks = await db.SubscriptionDonations
                .Where(link => nullableIds.Contains(link.DonationId) && EF.Property<int?>(link, "SubscriptionId") != null)
                .Select(link => new { link.DonationId, SubscriptionId = EF.Property<int?>(link, "SubscriptionId") })
                .ToListAsync(cancellationToken);
            var initialLinks = await db.Subscriptions
                .Where(subscription => nullableIds.Contains(EF.Property<int?>(subscription, "InitialDonationId")))
                .Select(subscription => new { DonationId = EF.Property<int?>(subscription, "InitialDonationId"), SubscriptionId = (int?)subscription.Id })
                .ToListAsync(cancellationToken);
            var links = subscriptionLinks.Concat(initialLinks).Distinct().ToList();
            List<int> subscriptionIds = links.Select(link => link.SubscriptionId!.Value).Distinct().ToList();
            var subscriptions = await db.Subscriptions
                .Where(subscription => subscriptionIds.Contains(subscription.Id))
                .Select(subscription => new
                {
                    subscription.Id,
                    subscription.Status,
                    subscription.SubscriptionType,
                    subscription.Frequency,
                    subscription.Created,
                    subscription.StartTime,
                    subscription.ExpirationTime,
                    subscription.EasyPaySubscriptionId,
                    subscription.TransactionKey,
                    subscription.IsDeleted,
                    InitialDonationId = EF.Property<int?>(subscription, "InitialDonationId"),
                    DonationCount = db.SubscriptionDonations.Count(link => EF.Property<int?>(link, "SubscriptionId") == subscription.Id),
                })
                .ToListAsync(cancellationToken);

            var relatedPayments = await db.Payments
                .Where(payment => nullableIds.Contains(EF.Property<int?>(payment, "DonationId"))
                    || db.Donations.Any(donation => donationIds.Contains(donation.Id) && EF.Property<int?>(donation, "ConfirmedPaymentId") == payment.Id))
                .OrderBy(payment => payment.Id)
                .Select(payment => new
                {
                    payment.Id,
                    PaymentType = EF.Property<string>(payment, "Discriminator"),
                    payment.Status,
                    payment.Created,
                    payment.Completed,
                    Paid = payment is CreditCardPayment ? (float?)((CreditCardPayment)payment).Paid
                        : payment is MBWayPayment ? (float?)((MBWayPayment)payment).Paid
                        : payment is MultiBankPayment ? (float?)((MultiBankPayment)payment).Paid
                        : null,
                    EasyPayPaymentId = payment is EasyPayBaseClass ? ((EasyPayBaseClass)payment).EasyPayPaymentId : null,
                    payment.TransactionKey,
                    DonationId = EF.Property<int?>(payment, "DonationId"),
                })
                .ToListAsync(cancellationToken);

            var result = new List<DonationDetail>();
            foreach (int id in donationIds)
            {
                var donation = donations.SingleOrDefault(row => row.Id == id);
                List<int> donationSubscriptionIds = links.Where(link => link.DonationId == id).Select(link => link.SubscriptionId!.Value).Distinct().ToList();
                result.Add(new DonationDetail
                {
                    Id = id,
                    Relations = relations[id],
                    Columns = rawRows.TryGetValue(id, out IReadOnlyList<KeyValuePair<string, string?>>? columns) ? columns : Array.Empty<KeyValuePair<string, string?>>(),
                    Donor = donation?.Donor == null
                        ? null
                        : new DonorInfo(donation.Donor.Id, donation.Donor.FullName, donation.Donor.Email, donation.Donor.Nif, donation.Donor.PhoneNumber, donation.Donor.CompanyName, donation.Donor.IsAnonymous),
                    FoodBankName = donation?.FoodBankName,
                    Items = items.Where(item => item.DonationId == id).Select(item => new DonationItemInfo(item.Id, item.ProductName, item.Quantity, item.Price)).ToList(),
                    Invoices = invoices.Where(invoice => invoice.DonationId == id).Select(invoice => invoice.Info).ToList(),
                    Subscriptions = subscriptions
                        .Where(subscription => donationSubscriptionIds.Contains(subscription.Id))
                        .Select(subscription => new SubscriptionInfo(
                            subscription.Id,
                            subscription.Status.ToString(),
                            subscription.SubscriptionType.ToString(),
                            subscription.Frequency,
                            subscription.Created,
                            subscription.StartTime,
                            subscription.ExpirationTime,
                            subscription.EasyPaySubscriptionId,
                            subscription.TransactionKey,
                            subscription.IsDeleted,
                            subscription.InitialDonationId,
                            subscription.DonationCount))
                        .ToList(),
                    Payments = relatedPayments
                        .Where(payment => payment.DonationId == id || payment.Id == donation?.ConfirmedPaymentId)
                        .Select(payment => new RelatedPaymentInfo(payment.Id, payment.PaymentType, payment.Status, payment.Created, payment.Completed, payment.Paid, payment.EasyPayPaymentId, payment.TransactionKey, payment.DonationId))
                        .ToList(),
                });
            }

            return result;
        }

        private static async Task<List<AuditEntry>> LoadAuditHistoryAsync(ApplicationDbContext db, List<int> paymentIds, AuditTablePresence audits, CancellationToken cancellationToken)
        {
            var entries = new List<AuditEntry>();
            if (audits.Reconciliation)
            {
                entries.AddRange(await SqlRunner.QueryAsync(
                    db.Database,
                    "SELECT PaymentId, CanonicalPaymentId, OriginalEasyPayPaymentId, ReconciledAtUtc FROM dbo.EasyPayPaymentIdReconciliationAudit WHERE PaymentId IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids));",
                    reader => new AuditEntry("Reconciliation", reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDateTime(3), null),
                    cancellationToken,
                    SqlRunner.Ids("@Ids", paymentIds)));
            }

            if (audits.Retirement)
            {
                entries.AddRange(await SqlRunner.QueryAsync(
                    db.Database,
                    "SELECT PaymentId, OriginalEasyPayPaymentId, RetiredAtUtc, PlanEvidenceHash FROM dbo.EasyPayPaymentIdRetirementAudit WHERE PaymentId IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids));",
                    reader => new AuditEntry("Retirement", reader.GetInt32(0), null, reader.GetString(1), reader.GetDateTime(2), reader.GetString(3)),
                    cancellationToken,
                    SqlRunner.Ids("@Ids", paymentIds)));
            }

            if (audits.Deletion)
            {
                entries.AddRange(await SqlRunner.QueryAsync(
                    db.Database,
                    "SELECT PaymentId, ReplacementPaymentId, OriginalEasyPayPaymentId, DeletedAtUtc, PlanEvidenceHash FROM dbo.EasyPayPaymentDeletionAudit WHERE PaymentId IN (SELECT CONVERT(int, value) FROM OPENJSON(@Ids));",
                    reader => new AuditEntry("Deletion", reader.GetInt32(0), SqlRunner.Get<int?>(reader, 1), reader.GetString(2), reader.GetDateTime(3), reader.GetString(4)),
                    cancellationToken,
                    SqlRunner.Ids("@Ids", paymentIds)));
            }

            return entries.OrderBy(entry => entry.AtUtc).ThenBy(entry => entry.PaymentId).ToList();
        }
    }
}
