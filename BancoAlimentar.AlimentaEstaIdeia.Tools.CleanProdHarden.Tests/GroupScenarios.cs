// -----------------------------------------------------------------------
// <copyright file="GroupScenarios.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis;

    /// <summary>
    /// Builds duplicate groups that mirror the shapes found in production data.
    /// </summary>
    internal static class GroupScenarios
    {
        /// <summary>
        /// The duplicated EasyPay payment ID shared by every scenario.
        /// </summary>
        internal const string ProviderId = "6bb9314a-957c-49e2-a743-8cd335c1157b";

        /// <summary>
        /// The time of the first payment attempt in every scenario.
        /// </summary>
        internal static readonly DateTime T0 = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Two unlinked webhook captures plus a newer row inserted already completed and confirmed by the donation.
        /// </summary>
        internal static GroupEvaluation ReconciliationToolCopy()
        {
            var donation = new DonationFacts(500, PaymentStatus.Payed, 10.0, 102);
            return DuplicateGroupRules.EvaluateGroup(new[]
            {
                Payment(100, created: T0, completed: T0.AddMinutes(1), status: "ok", paid: 10f),
                Payment(101, created: T0.AddMinutes(2), completed: T0.AddMinutes(3), status: "ok", paid: 10f),
                Payment(102, created: T0.AddDays(1), completed: T0.AddDays(1).AddSeconds(1), status: "ok", paid: 10f, donationId: 500, forward: donation, confirming: new[] { donation }),
            });
        }

        /// <summary>
        /// A confirmed paid payment plus zero-value, uncompleted retries for the same donation.
        /// </summary>
        internal static GroupEvaluation ZeroValueRetries()
        {
            var donation = new DonationFacts(600, PaymentStatus.Payed, 25.0, 200);
            return DuplicateGroupRules.EvaluateGroup(new[]
            {
                Payment(200, created: T0, completed: T0.AddMinutes(10), status: "Success", paid: 25f, donationId: 600, forward: donation, confirming: new[] { donation }),
                Payment(201, created: T0.AddMinutes(1), status: "Failed", paid: 0f, donationId: 600, forward: donation),
                Payment(202, created: T0.AddMinutes(2), status: "Failed", paid: 0f, donationId: 600, forward: donation),
            });
        }

        /// <summary>
        /// Failed attempts whose donation was paid by a different, successful payment outside the group.
        /// </summary>
        internal static GroupEvaluation SupersededAttempts()
        {
            var donation = new DonationFacts(700, PaymentStatus.Payed, 15.0, 999);
            var replacement = new ReplacementPaymentFacts(999, "MBWayPayment", "Success", 15f, 700, null, null);
            return DuplicateGroupRules.EvaluateGroup(new[]
            {
                Payment(300, created: T0, status: "Failed", paid: 0f, donationId: 700, forward: donation, replacement: replacement),
                Payment(301, created: T0.AddMinutes(1), status: "Failed", paid: 0f, donationId: 700, forward: donation, replacement: replacement),
            });
        }

        /// <summary>
        /// Attempts without a donation link, status, completion, or paid amount.
        /// </summary>
        internal static GroupEvaluation AbandonedOrphans(float? paid = 0f)
        {
            return DuplicateGroupRules.EvaluateGroup(new[]
            {
                Payment(400, created: T0, paid: paid),
                Payment(401, created: T0.AddSeconds(30), paid: paid),
            });
        }

        /// <summary>
        /// Two rows created in the same second with one transaction key, each confirming a different donation.
        /// </summary>
        internal static GroupEvaluation SimultaneousCharges(bool sameDay = true)
        {
            var first = new DonationFacts(800, PaymentStatus.Payed, 5.0, 500);
            var second = new DonationFacts(801, PaymentStatus.Payed, 5.0, 501);
            DateTime secondCreated = sameDay ? T0 : T0.AddDays(31);
            return DuplicateGroupRules.EvaluateGroup(new[]
            {
                Payment(500, created: T0, completed: T0, paid: 5f, transactionKey: "TK-1", donationId: 800, forward: first, confirming: new[] { first }),
                Payment(501, created: secondCreated, completed: secondCreated, paid: 5f, transactionKey: "TK-1", donationId: 801, forward: second, confirming: new[] { second }),
            });
        }

        /// <summary>
        /// Creates one member input.
        /// </summary>
        internal static PaymentMemberInput Payment(
            int id,
            DateTime created,
            DateTime? completed = null,
            string? status = null,
            float? paid = 0f,
            float? requested = null,
            string? transactionKey = null,
            int? donationId = null,
            DonationFacts? forward = null,
            IReadOnlyList<DonationFacts>? confirming = null,
            ReplacementPaymentFacts? replacement = null,
            string providerId = ProviderId)
        {
            return new PaymentMemberInput
            {
                PaymentId = id,
                EasyPayPaymentId = providerId,
                PaymentType = "MBWayPayment",
                Created = created,
                Completed = completed,
                Status = status,
                TransactionKey = transactionKey,
                Requested = requested ?? paid,
                Paid = paid,
                DonationId = donationId,
                ForwardDonation = forward,
                ConfirmingDonations = confirming ?? Array.Empty<DonationFacts>(),
                Replacement = replacement,
            };
        }
    }
}
