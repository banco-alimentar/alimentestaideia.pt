// -----------------------------------------------------------------------
// <copyright file="SubscriptionDonationOwnership.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay
{
    using System;

    /// <summary>
    /// Applies the ownership rules for matching a local donation to a subscription payment.
    /// </summary>
    public static class SubscriptionDonationOwnership
    {
        /// <summary>
        /// Determines whether a matched donation may be completed for a subscription.
        /// </summary>
        /// <param name="isInitialDonation">Whether the donation is the subscription's initial donation.</param>
        /// <param name="isLinkedToTargetSubscription">Whether the donation is linked to the target subscription.</param>
        /// <param name="isLinkedToAnotherSubscription">Whether the donation is linked to another subscription.</param>
        /// <param name="paymentTransactionKey">Transaction key on the matched local payment.</param>
        /// <param name="subscriptionTransactionKey">Transaction key on the target subscription.</param>
        /// <returns><see langword="true" /> when the donation is eligible for the target subscription.</returns>
        public static bool IsEligible(
            bool isInitialDonation,
            bool isLinkedToTargetSubscription,
            bool isLinkedToAnotherSubscription,
            string paymentTransactionKey,
            string subscriptionTransactionKey)
        {
            if (isLinkedToAnotherSubscription)
            {
                return false;
            }

            if (isInitialDonation || isLinkedToTargetSubscription)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(paymentTransactionKey)
                && string.Equals(paymentTransactionKey, subscriptionTransactionKey, StringComparison.Ordinal);
        }
    }
}
