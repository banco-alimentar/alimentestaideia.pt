// -----------------------------------------------------------------------
// <copyright file="EasyPaySubscriptionTransactionSelector.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Selects a verified paid transaction from an Easypay subscription response.
    /// </summary>
    public static class EasyPaySubscriptionTransactionSelector
    {
        /// <summary>
        /// Selects the embedded transaction represented by a subscription callback.
        /// </summary>
        /// <param name="subscription">Subscription response from Easypay.</param>
        /// <param name="notificationId">Identifier supplied in the webhook callback.</param>
        /// <param name="notificationKey">Merchant transaction key supplied in the callback.</param>
        /// <param name="notificationDate">Capture date supplied in the callback.</param>
        /// <param name="expectedAmount">Donation amount stored locally.</param>
        /// <returns>A unique verified transaction or a stable rejection reason.</returns>
        public static SubscriptionTransactionMatchResult Select(
            SubscriptionIdGet200Response subscription,
            Guid notificationId,
            string notificationKey,
            DateTime notificationDate,
            double expectedAmount)
        {
            if (subscription == null || subscription.Id == default)
            {
                return SubscriptionTransactionMatchResult.Rejected("easypay_subscription_not_found");
            }

            if (string.IsNullOrWhiteSpace(notificationKey)
                || !string.Equals(subscription.Key, notificationKey, StringComparison.Ordinal))
            {
                return SubscriptionTransactionMatchResult.Rejected("subscription_key_mismatch");
            }

            if (!PaymentAmountReconciliation.ProviderValueMatchesDonation(expectedAmount, (double)subscription.Value))
            {
                return SubscriptionTransactionMatchResult.Rejected("subscription_value_mismatch");
            }

            if (subscription.Transactions == null || subscription.Transactions.Count == 0)
            {
                return SubscriptionTransactionMatchResult.Rejected("subscription_transactions_missing");
            }

            string notificationIdValue = notificationId == default ? null : notificationId.ToString();
            string providerSubscriptionId = subscription.Id.ToString();
            if (string.Equals(notificationIdValue, providerSubscriptionId, StringComparison.OrdinalIgnoreCase))
            {
                var fallbackMatches = subscription.Transactions
                    .Where(transaction => IsValidPaidTransaction(transaction, subscription.Id, notificationKey, expectedAmount))
                    .Where(transaction => TryGetTransactionDate(transaction, out DateTime transactionDate)
                        && transactionDate.Date == notificationDate.Date)
                    .ToList();

                if (fallbackMatches.Count == 1)
                {
                    return SubscriptionTransactionMatchResult.Matched(
                        ToEvidence(subscription, fallbackMatches[0]));
                }

                return SubscriptionTransactionMatchResult.Rejected(
                    fallbackMatches.Count > 1
                        ? "subscription_transaction_ambiguous"
                        : "subscription_transaction_not_found");
            }

            if (string.IsNullOrWhiteSpace(notificationIdValue))
            {
                return SubscriptionTransactionMatchResult.Rejected("subscription_transaction_id_missing");
            }

            List<SubscriptionIdGet200ResponseTransactionsInner> exactMatches = subscription.Transactions
                .Where(transaction => string.Equals(
                    transaction?.Id,
                    notificationIdValue,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactMatches.Count == 0)
            {
                return SubscriptionTransactionMatchResult.Rejected("subscription_transaction_unknown_callback_id");
            }

            if (exactMatches.Count > 1)
            {
                return SubscriptionTransactionMatchResult.Rejected("subscription_transaction_ambiguous");
            }

            string validationFailure = ValidateTransaction(
                exactMatches[0],
                subscription.Id,
                notificationKey,
                notificationDate,
                expectedAmount);
            return validationFailure == null
                ? SubscriptionTransactionMatchResult.Matched(ToEvidence(subscription, exactMatches[0]))
                : SubscriptionTransactionMatchResult.Rejected(validationFailure);
        }

        private static bool IsValidPaidTransaction(
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            Guid subscriptionId,
            string notificationKey,
            double expectedAmount)
        {
            if (transaction == null
                || string.IsNullOrWhiteSpace(transaction.Id)
                || string.Equals(transaction.Id, subscriptionId.ToString(), StringComparison.OrdinalIgnoreCase)
                || transaction.Values == null
                || transaction.Values.Requested <= 0
                || transaction.Values.Paid <= 0)
            {
                return false;
            }

            return ValidateTransaction(transaction, subscriptionId, notificationKey, DateTime.MinValue, expectedAmount) == null;
        }

        private static string ValidateTransaction(
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            Guid subscriptionId,
            string notificationKey,
            DateTime notificationDate,
            double expectedAmount)
        {
            if (transaction == null || string.IsNullOrWhiteSpace(transaction.Id))
            {
                return "subscription_transaction_id_missing";
            }

            if (string.Equals(transaction.Id, subscriptionId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return "subscription_transaction_id_is_subscription_id";
            }

            if (string.IsNullOrWhiteSpace(transaction.Key))
            {
                return "subscription_transaction_key_missing";
            }

            if (!string.Equals(transaction.Key, notificationKey, StringComparison.Ordinal))
            {
                return "subscription_transaction_key_mismatch";
            }

            if (transaction.Values == null
                || transaction.Values.Requested <= 0
                || transaction.Values.Paid <= 0)
            {
                return "subscription_transaction_not_paid";
            }

            if (!PaymentAmountReconciliation.AmountsMatchDonation(
                    expectedAmount,
                    (double)transaction.Values.Requested,
                    (double)transaction.Values.Paid))
            {
                return "subscription_transaction_amount_mismatch";
            }

            if (!TryGetTransactionDate(transaction, out DateTime transactionDate))
            {
                return "subscription_transaction_date_missing";
            }

            if (notificationDate != DateTime.MinValue && transactionDate.Date != notificationDate.Date)
            {
                return "subscription_transaction_date_mismatch";
            }

            return null;
        }

        private static bool TryGetTransactionDate(
            SubscriptionIdGet200ResponseTransactionsInner transaction,
            out DateTime transactionDate)
        {
            transactionDate = default;
            string[] dateValues = { transaction.Date, transaction.CreatedAt };
            foreach (string dateValue in dateValues)
            {
                if (DateTime.TryParse(
                    dateValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out transactionDate))
                {
                    return true;
                }
            }

            return false;
        }

        private static EasyPaySubscriptionPaymentEvidence ToEvidence(
            SubscriptionIdGet200Response subscription,
            SubscriptionIdGet200ResponseTransactionsInner transaction)
        {
            TryGetTransactionDate(transaction, out DateTime paymentDate);

            return new EasyPaySubscriptionPaymentEvidence
            {
                EasypaySubscriptionId = subscription.Id.ToString(),
                EasypayPaymentId = transaction.Id,
                TransactionKey = transaction.Key,
                PaymentDate = paymentDate,
                Requested = transaction.Values.Requested,
                Paid = transaction.Values.Paid,
                FixedFee = transaction.Values.FixedFee,
                VariableFee = transaction.Values.VariableFee,
                Tax = transaction.Values.Tax,
                Transfer = transaction.Values.Transfer,
            };
        }
    }
}
