// -----------------------------------------------------------------------
// <copyright file="SubscriptionTransactionMatchResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay
{
    /// <summary>
    /// Result of matching a subscription callback to an embedded transaction.
    /// </summary>
    public sealed class SubscriptionTransactionMatchResult
    {
        private SubscriptionTransactionMatchResult(
            bool isMatch,
            string failureReason,
            EasyPaySubscriptionPaymentEvidence evidence)
        {
            this.IsMatch = isMatch;
            this.FailureReason = failureReason;
            this.Evidence = evidence;
        }

        /// <summary>
        /// Gets a value indicating whether a unique transaction was selected.
        /// </summary>
        public bool IsMatch { get; }

        /// <summary>
        /// Gets the stable reason when no transaction was selected.
        /// </summary>
        public string FailureReason { get; }

        /// <summary>
        /// Gets the verified payment evidence when <see cref="IsMatch"/> is true.
        /// </summary>
        public EasyPaySubscriptionPaymentEvidence Evidence { get; }

        /// <summary>
        /// Creates a successful match result.
        /// </summary>
        /// <param name="evidence">Verified provider evidence.</param>
        /// <returns>Successful result.</returns>
        public static SubscriptionTransactionMatchResult Matched(EasyPaySubscriptionPaymentEvidence evidence)
        {
            return new SubscriptionTransactionMatchResult(true, null, evidence);
        }

        /// <summary>
        /// Creates a failed match result.
        /// </summary>
        /// <param name="reason">Stable failure reason.</param>
        /// <returns>Failed result.</returns>
        public static SubscriptionTransactionMatchResult Rejected(string reason)
        {
            return new SubscriptionTransactionMatchResult(false, reason ?? "subscription_transaction_not_found", null);
        }
    }
}
