// -----------------------------------------------------------------------
// <copyright file="EasyPaySubscriptionTransactionSelectorTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.ObjectModel;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using Easypay.Rest.Client.Model;
    using Xunit;

    /// <summary>
    /// Tests for matching subscription callbacks to embedded Easypay transactions.
    /// </summary>
    public class EasyPaySubscriptionTransactionSelectorTests
    {
        /// <summary>
        /// Selects the exact embedded payment when the callback contains its id.
        /// </summary>
        [Fact]
        public void SelectsExactPaymentId()
        {
            Guid subscriptionId = Guid.NewGuid();
            Guid paymentId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                paymentId,
                paymentDate,
                new[] { paymentId.ToString() });

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                paymentId,
                "transaction-key",
                paymentDate.AddHours(1),
                5.40d);

            Assert.True(result.IsMatch);
            Assert.Equal(paymentId.ToString(), result.Evidence.EasypayPaymentId);
        }

        /// <summary>
        /// Selects the unique embedded payment when the callback contains the subscription id.
        /// </summary>
        [Fact]
        public void SelectsUniquePaymentWhenCallbackContainsSubscriptionId()
        {
            Guid subscriptionId = Guid.NewGuid();
            Guid paymentId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                paymentId,
                paymentDate,
                new[] { paymentId.ToString() });

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                subscriptionId,
                "transaction-key",
                paymentDate,
                5.40d);

            Assert.True(result.IsMatch);
            Assert.Equal(paymentId.ToString(), result.Evidence.EasypayPaymentId);
            Assert.NotEqual(subscriptionId.ToString(), result.Evidence.EasypayPaymentId);
        }

        /// <summary>
        /// Rejects a callback when multiple embedded payments match its date and amount.
        /// </summary>
        [Fact]
        public void RejectsAmbiguousSubscriptionIdCallback()
        {
            Guid subscriptionId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                Guid.NewGuid(),
                paymentDate,
                new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() });

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                subscriptionId,
                "transaction-key",
                paymentDate,
                5.40d);

            Assert.False(result.IsMatch);
            Assert.Equal("subscription_transaction_ambiguous", result.FailureReason);
        }

        /// <summary>
        /// Rejects a zero-value embedded transaction.
        /// </summary>
        [Fact]
        public void RejectsZeroValueTransaction()
        {
            Guid subscriptionId = Guid.NewGuid();
            Guid paymentId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = new SubscriptionIdGet200Response(
                id: subscriptionId,
                key: "transaction-key",
                value: 5.40m,
                transactions: new Collection<SubscriptionIdGet200ResponseTransactionsInner>
                {
                    new SubscriptionIdGet200ResponseTransactionsInner(
                        id: paymentId.ToString(),
                        key: "transaction-key",
                        date: paymentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        values: new SubscriptionIdGet200ResponseTransactionsInnerValues()),
                });

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                paymentId,
                "transaction-key",
                paymentDate,
                5.40d);

            Assert.False(result.IsMatch);
            Assert.Equal("subscription_transaction_not_paid", result.FailureReason);
        }

        /// <summary>
        /// Does not use another transaction when the callback identifies an invalid exact transaction.
        /// </summary>
        [Fact]
        public void RejectsInvalidExactTransactionWithoutFallback()
        {
            Guid subscriptionId = Guid.NewGuid();
            Guid callbackPaymentId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                callbackPaymentId,
                paymentDate,
                new[] { callbackPaymentId.ToString(), Guid.NewGuid().ToString() });
            subscription.Transactions[0].Values.Paid = 0;

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                callbackPaymentId,
                "transaction-key",
                paymentDate,
                5.40d);

            Assert.False(result.IsMatch);
            Assert.Equal("subscription_transaction_not_paid", result.FailureReason);
        }

        /// <summary>
        /// Rejects a callback whose identifier is not a subscription or embedded transaction identifier.
        /// </summary>
        [Fact]
        public void RejectsUnknownCallbackId()
        {
            Guid subscriptionId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                Guid.NewGuid(),
                paymentDate,
                new[] { Guid.NewGuid().ToString() });

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                Guid.NewGuid(),
                "transaction-key",
                paymentDate,
                5.40d);

            Assert.False(result.IsMatch);
            Assert.Equal("subscription_transaction_unknown_callback_id", result.FailureReason);
        }

        /// <summary>
        /// Rejects an embedded transaction with no provider transaction key.
        /// </summary>
        [Fact]
        public void RejectsMissingProviderTransactionKey()
        {
            Guid subscriptionId = Guid.NewGuid();
            Guid paymentId = Guid.NewGuid();
            DateTime paymentDate = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                paymentId,
                paymentDate,
                new[] { paymentId.ToString() });
            subscription.Transactions[0].Key = null;

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                paymentId,
                "transaction-key",
                paymentDate,
                5.40d);

            Assert.False(result.IsMatch);
            Assert.Equal("subscription_transaction_key_missing", result.FailureReason);
        }

        /// <summary>
        /// Uses CreatedAt for evidence when Date is present but invalid.
        /// </summary>
        [Fact]
        public void UsesCreatedAtForEvidenceWhenDateIsInvalid()
        {
            Guid subscriptionId = Guid.NewGuid();
            Guid paymentId = Guid.NewGuid();
            DateTime createdAt = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            SubscriptionIdGet200Response subscription = this.CreateSubscription(
                subscriptionId,
                paymentId,
                createdAt,
                new[] { paymentId.ToString() });
            subscription.Transactions[0].Date = "not-a-date";
            subscription.Transactions[0].CreatedAt = createdAt.ToString("O");

            SubscriptionTransactionMatchResult result = EasyPaySubscriptionTransactionSelector.Select(
                subscription,
                paymentId,
                "transaction-key",
                createdAt,
                5.40d);

            Assert.True(result.IsMatch);
            Assert.Equal(createdAt, result.Evidence.PaymentDate);
        }

        private SubscriptionIdGet200Response CreateSubscription(
            Guid subscriptionId,
            Guid paymentId,
            DateTime paymentDate,
            string[] paymentIds)
        {
            var transactions = new Collection<SubscriptionIdGet200ResponseTransactionsInner>();
            foreach (string id in paymentIds)
            {
                transactions.Add(new SubscriptionIdGet200ResponseTransactionsInner(
                    id: id,
                    key: "transaction-key",
                    date: paymentDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    values: new SubscriptionIdGet200ResponseTransactionsInnerValues(
                        requested: 5.40m,
                        paid: 5.40m)));
            }

            return new SubscriptionIdGet200Response(
                id: subscriptionId,
                key: "transaction-key",
                value: 5.40m,
                transactions: transactions);
        }
    }
}
