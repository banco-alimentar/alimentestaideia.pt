// -----------------------------------------------------------------------
// <copyright file="SubscriptionTransactionMatcherTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay;
    using Easypay.Rest.Client.Model;
    using Xunit;

    /// <summary>
    /// Tests the tools-local embedded subscription transaction rules.
    /// </summary>
    public sealed class SubscriptionTransactionMatcherTests
    {
        [Fact]
        public void OrderNewestFirst_ReturnsNewestTransactionFirst()
        {
            List<SubscriptionIdGet200ResponseTransactionsInner> transactions = new()
            {
                CreateTransaction("older", "2026-08-01T10:00:00Z", 5.40m),
                CreateTransaction("newer", "2026-09-01T10:00:00Z", 5.40m),
            };

            IReadOnlyList<SubscriptionIdGet200ResponseTransactionsInner> result =
                SubscriptionTransactionMatcher.OrderNewestFirst(transactions);

            Assert.Equal("newer", result[0].Id);
            Assert.Equal("older", result[1].Id);
        }

        [Fact]
        public void Select_WhenCallbackContainsSubscriptionId_UsesUniqueEmbeddedPaidTransaction()
        {
            const string subscriptionId = "11111111-1111-1111-1111-111111111111";
            const string transactionKey = "merchant-key";
            SubscriptionIdGet200ResponseTransactionsInner transaction = CreateTransaction(
                "22222222-2222-2222-2222-222222222222",
                "2026-09-01T10:00:00Z",
                5.40m,
                transactionKey);

            SubscriptionTransactionSelection result = SubscriptionTransactionMatcher.Select(
                new[] { transaction },
                subscriptionId,
                subscriptionId,
                transactionKey,
                new DateTime(2026, 9, 1),
                5.40);

            Assert.True(result.IsSuccess);
            Assert.True(result.UsedSubscriptionIdFallback);
            Assert.Equal(transaction.Id, result.Transaction.Id);
        }

        [Fact]
        public void Select_WhenFallbackHasMultipleTransactions_ReportsAmbiguity()
        {
            const string subscriptionId = "11111111-1111-1111-1111-111111111111";
            const string transactionKey = "merchant-key";
            SubscriptionTransactionSelection result = SubscriptionTransactionMatcher.Select(
                new[]
                {
                    CreateTransaction("22222222-2222-2222-2222-222222222222", "2026-09-01T10:00:00Z", 5.40m, transactionKey),
                    CreateTransaction("33333333-3333-3333-3333-333333333333", "2026-09-01T11:00:00Z", 5.40m, transactionKey),
                },
                subscriptionId,
                subscriptionId,
                transactionKey,
                new DateTime(2026, 9, 1),
                5.40);

            Assert.False(result.IsSuccess);
            Assert.Equal("subscription_transaction_ambiguous", result.FailureReason);
        }

        [Fact]
        public void IsPaid_RejectsZeroValueTransaction()
        {
            SubscriptionIdGet200ResponseTransactionsInner transaction = CreateTransaction(
                "22222222-2222-2222-2222-222222222222",
                "2026-09-01T10:00:00Z",
                0m);

            Assert.False(SubscriptionTransactionMatcher.IsPaid(transaction, 5.40));
        }

        [Fact]
        public void ExactPaymentMatchesAreReservedBeforeNewerFallbackItems()
        {
            List<(string Id, DateTime Date)> payments = new()
            {
                ("fallback", new DateTime(2026, 9, 2)),
                ("exact", new DateTime(2026, 9, 1)),
            };
            ISet<string> reservedPaymentIds = SubscriptionTransactionMatcher.ReserveExactPaymentIds(
                payments.Select(payment => payment.Id),
                new[] { "exact" });

            IReadOnlyList<(string Id, DateTime Date)> result =
                SubscriptionTransactionMatcher.OrderExactMatchesFirst(
                    payments,
                    reservedPaymentIds,
                    payment => payment.Id,
                    payment => payment.Date);

            Assert.Equal("exact", result[0].Id);
            Assert.Equal("fallback", result[1].Id);
        }

        [Fact]
        public void UnlinkedPlaceholderIsEligibleOnlyWithExactSubscriptionTransactionKey()
        {
            Assert.True(SubscriptionDonationOwnership.IsEligible(
                isInitialDonation: false,
                isLinkedToTargetSubscription: false,
                isLinkedToAnotherSubscription: false,
                paymentTransactionKey: "subscription-key",
                subscriptionTransactionKey: "subscription-key"));

            Assert.False(SubscriptionDonationOwnership.IsEligible(
                isInitialDonation: false,
                isLinkedToTargetSubscription: false,
                isLinkedToAnotherSubscription: false,
                paymentTransactionKey: "other-key",
                subscriptionTransactionKey: "subscription-key"));
        }

        [Fact]
        public void AlreadyPaidDonationWithDifferentProviderPaymentIdIsNotEligibleForReconciliation()
        {
            Assert.False(SubscriptionTransactionMatcher.CanReconcileSelectedPayment(
                donationAlreadyPaid: true,
                selectedPaymentIsExactProviderPaymentIdMatch: false));
        }

        [Fact]
        public void ExactProviderPaymentIdMatchRemainsEligibleForAlreadyPaidDonation()
        {
            Assert.True(SubscriptionTransactionMatcher.CanReconcileSelectedPayment(
                donationAlreadyPaid: true,
                selectedPaymentIsExactProviderPaymentIdMatch: true));
        }

        [Fact]
        public void UnpaidSinglePaymentPlaceholderRemainsEligibleWithoutExactProviderPaymentIdMatch()
        {
            Assert.True(SubscriptionTransactionMatcher.CanReconcileSelectedPayment(
                donationAlreadyPaid: false,
                selectedPaymentIsExactProviderPaymentIdMatch: false));
        }

        [Fact]
        public void DonationLinkedToAnotherSubscriptionIsRejected()
        {
            Assert.False(SubscriptionDonationOwnership.IsEligible(
                isInitialDonation: false,
                isLinkedToTargetSubscription: false,
                isLinkedToAnotherSubscription: true,
                paymentTransactionKey: "subscription-key",
                subscriptionTransactionKey: "subscription-key"));
        }

        private static SubscriptionIdGet200ResponseTransactionsInner CreateTransaction(
            string id,
            string date,
            decimal amount,
            string key = "merchant-key")
        {
            return new SubscriptionIdGet200ResponseTransactionsInner(
                id: id,
                key: key,
                date: date,
                values: new SubscriptionIdGet200ResponseTransactionsInnerValues(
                    requested: amount,
                    paid: amount));
        }
    }
}
