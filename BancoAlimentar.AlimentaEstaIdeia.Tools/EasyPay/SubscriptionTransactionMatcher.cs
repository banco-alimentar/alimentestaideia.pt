// -----------------------------------------------------------------------
// <copyright file="SubscriptionTransactionMatcher.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Contains the tools-local rules for validating and selecting embedded Easypay subscription transactions.
    /// This is intentionally local to the tools project until a shared selector contract is available.
    /// </summary>
    public static class SubscriptionTransactionMatcher
    {
        /// <summary>
        /// Orders embedded provider transactions from newest to oldest.
        /// </summary>
        /// <param name="transactions">Provider transactions.</param>
        /// <returns>Transactions ordered from newest to oldest.</returns>
        public static IReadOnlyList<SubscriptionIdGet200ResponseTransactionsInner> OrderNewestFirst(
            IEnumerable<SubscriptionIdGet200ResponseTransactionsInner> transactions)
        {
            return (transactions ?? Array.Empty<SubscriptionIdGet200ResponseTransactionsInner>())
                .Where(transaction => transaction != null)
                .OrderByDescending(GetTransactionDate)
                .ThenByDescending(
                    transaction => transaction.Id,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Returns reconciliation items with exact provider-id matches before fallback candidates.
        /// </summary>
        /// <typeparam name="TItem">Reconciliation item type.</typeparam>
        /// <param name="items">Reconciliation items.</param>
        /// <param name="reservedPaymentIds">Provider payment IDs already present locally.</param>
        /// <param name="getPaymentId">Gets the provider payment ID.</param>
        /// <param name="getPaymentDate">Gets the provider payment date.</param>
        /// <returns>Items ordered with reserved exact matches first, then newest first.</returns>
        public static IReadOnlyList<TItem> OrderExactMatchesFirst<TItem>(
            IEnumerable<TItem> items,
            ISet<string> reservedPaymentIds,
            Func<TItem, string> getPaymentId,
            Func<TItem, DateTime> getPaymentDate)
        {
            return (items ?? Array.Empty<TItem>())
                .OrderByDescending(item => reservedPaymentIds != null
                    && !string.IsNullOrWhiteSpace(getPaymentId(item))
                    && reservedPaymentIds.Contains(getPaymentId(item)))
                .ThenByDescending(getPaymentDate)
                .ThenByDescending(
                    getPaymentId,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Finds provider payment IDs that are already stored locally.
        /// </summary>
        /// <param name="providerPaymentIds">Provider payment IDs returned by Easypay.</param>
        /// <param name="localPaymentIds">Provider payment IDs stored in local payments.</param>
        /// <returns>The provider IDs that have an exact local match.</returns>
        public static ISet<string> ReserveExactPaymentIds(
            IEnumerable<string> providerPaymentIds,
            IEnumerable<string> localPaymentIds)
        {
            HashSet<string> localIds = new HashSet<string>(
                (localPaymentIds ?? Array.Empty<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);

            return new HashSet<string>(
                (providerPaymentIds ?? Array.Empty<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Where(localIds.Contains),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the selected local payment may be used for reconciliation.
        /// </summary>
        /// <param name="donationAlreadyPaid">Whether the donation is already paid or has a confirmed payment.</param>
        /// <param name="selectedPaymentIsExactProviderPaymentIdMatch">Whether the selected payment has the provider payment ID.</param>
        /// <returns>True when the selected payment may be reconciled.</returns>
        public static bool CanReconcileSelectedPayment(
            bool donationAlreadyPaid,
            bool selectedPaymentIsExactProviderPaymentIdMatch)
        {
            return !donationAlreadyPaid || selectedPaymentIsExactProviderPaymentIdMatch;
        }

        /// <summary>
        /// Determines whether the embedded transaction is a paid transaction for the expected amount.
        /// </summary>
        /// <param name="transaction">Provider transaction.</param>
        /// <param name="expectedAmount">Expected donation amount.</param>
        /// <returns>True when requested and paid values are positive and match the expected amount.</returns>
        public static bool IsPaid(
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            double expectedAmount)
        {
            return transaction?.Values != null
                && transaction.Values.Requested > 0
                && transaction.Values.Paid > 0
                && PaymentAmountReconciliation.AmountsMatchDonation(
                    expectedAmount,
                    (double)transaction.Values.Requested,
                    (double)transaction.Values.Paid);
        }

        /// <summary>
        /// Selects an embedded transaction for a subscription callback.
        /// </summary>
        /// <param name="transactions">Provider transactions.</param>
        /// <param name="notificationId">Identifier received in the callback.</param>
        /// <param name="subscriptionId">Provider subscription identifier.</param>
        /// <param name="transactionKey">Expected merchant transaction key.</param>
        /// <param name="notificationDate">Date received in the callback.</param>
        /// <param name="expectedAmount">Expected donation amount.</param>
        /// <returns>A selection result with an explicit reason when no transaction can be selected.</returns>
        public static SubscriptionTransactionSelection Select(
            IEnumerable<SubscriptionIdGet200ResponseTransactionsInner> transactions,
            string notificationId,
            string subscriptionId,
            string transactionKey,
            DateTime notificationDate,
            double expectedAmount)
        {
            IReadOnlyList<SubscriptionIdGet200ResponseTransactionsInner> orderedTransactions =
                OrderNewestFirst(transactions);
            SubscriptionIdGet200ResponseTransactionsInner exactMatch = orderedTransactions
                .FirstOrDefault(transaction => string.Equals(
                    transaction.Id,
                    notificationId,
                    StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return ValidateSelectedTransaction(
                    exactMatch,
                    transactionKey,
                    subscriptionId,
                    notificationDate,
                    expectedAmount,
                    false);
            }

            if (!string.Equals(notificationId, subscriptionId, StringComparison.OrdinalIgnoreCase))
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_not_found");
            }

            List<SubscriptionIdGet200ResponseTransactionsInner> candidates = orderedTransactions
                .Where(transaction => string.Equals(transaction.Key, transactionKey, StringComparison.Ordinal))
                .Where(transaction => GetTransactionDate(transaction).Date == notificationDate.Date)
                .Where(transaction => IsPaid(transaction, expectedAmount))
                .ToList();

            if (candidates.Count == 0)
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_not_found");
            }

            if (candidates.Count > 1)
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_ambiguous");
            }

            return ValidateSelectedTransaction(
                candidates[0],
                transactionKey,
                subscriptionId,
                notificationDate,
                expectedAmount,
                true);
        }

        /// <summary>
        /// Gets the first usable provider date from the embedded transaction.
        /// </summary>
        /// <param name="transaction">Provider transaction.</param>
        /// <returns>The parsed provider date, or <see cref="DateTime.MinValue"/> when unavailable.</returns>
        public static DateTime GetTransactionDate(SubscriptionIdGet200ResponseTransactionsInner transaction)
        {
            if (transaction == null)
            {
                return DateTime.MinValue;
            }

            foreach (string value in new[] { transaction.Date, transaction.CreatedAt, transaction.TransferDate })
            {
                if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                    out DateTime parsed))
                {
                    return parsed;
                }
            }

            return DateTime.MinValue;
        }

        private static SubscriptionTransactionSelection ValidateSelectedTransaction(
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            string transactionKey,
            string subscriptionId,
            DateTime notificationDate,
            double expectedAmount,
            bool usedSubscriptionIdFallback)
        {
            if (string.IsNullOrWhiteSpace(transaction.Id))
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_id_missing");
            }

            if (string.Equals(transaction.Id, subscriptionId, StringComparison.OrdinalIgnoreCase))
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_id_is_subscription_id");
            }

            if (!string.Equals(transaction.Key, transactionKey, StringComparison.Ordinal))
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_key_mismatch");
            }

            DateTime transactionDate = GetTransactionDate(transaction);
            if (transactionDate == DateTime.MinValue || transactionDate.Date != notificationDate.Date)
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_date_mismatch");
            }

            if (!IsPaid(transaction, expectedAmount))
            {
                return SubscriptionTransactionSelection.Failure("subscription_transaction_not_paid");
            }

            return SubscriptionTransactionSelection.Success(transaction, usedSubscriptionIdFallback);
        }
    }

    /// <summary>
    /// Result of selecting an embedded subscription transaction.
    /// </summary>
    public sealed class SubscriptionTransactionSelection
    {
        private SubscriptionTransactionSelection(
            bool isSuccess,
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            string failureReason,
            bool usedSubscriptionIdFallback)
        {
            this.IsSuccess = isSuccess;
            this.Transaction = transaction;
            this.FailureReason = failureReason;
            this.UsedSubscriptionIdFallback = usedSubscriptionIdFallback;
        }

        /// <summary>
        /// Gets a value indicating whether a transaction was selected.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the selected transaction.
        /// </summary>
        public SubscriptionIdGet200ResponseTransactionsInner Transaction { get; }

        /// <summary>
        /// Gets the stable failure reason, when selection failed.
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Gets a value indicating whether the callback used the subscription ID and selected an embedded transaction.
        /// </summary>
        public bool UsedSubscriptionIdFallback { get; }

        /// <summary>
        /// Creates a successful selection result.
        /// </summary>
        /// <param name="transaction">Selected transaction.</param>
        /// <param name="usedSubscriptionIdFallback">Whether fallback selection was used.</param>
        /// <returns>A successful result.</returns>
        public static SubscriptionTransactionSelection Success(
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            bool usedSubscriptionIdFallback)
        {
            return new SubscriptionTransactionSelection(true, transaction, null, usedSubscriptionIdFallback);
        }

        /// <summary>
        /// Creates a failed selection result.
        /// </summary>
        /// <param name="failureReason">Stable failure reason.</param>
        /// <returns>A failed result.</returns>
        public static SubscriptionTransactionSelection Failure(string failureReason)
        {
            return new SubscriptionTransactionSelection(false, null, failureReason, false);
        }
    }
}
