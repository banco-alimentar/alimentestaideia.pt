// -----------------------------------------------------------------------
// <copyright file="GroupPatternClassifier.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Linq;

    /// <summary>
    /// Matches a group to the historical pattern that most likely created it.
    /// </summary>
    public static class GroupPatternClassifier
    {
        /// <summary>
        /// Maximum gap between Created and Completed for a row inserted already completed by a tool.
        /// </summary>
        public static readonly TimeSpan ToolCreatedCompletedTolerance = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Maximum Created spread for charges considered simultaneous.
        /// </summary>
        public static readonly TimeSpan SimultaneousTolerance = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Classifies a group.
        /// </summary>
        /// <param name="group">Evaluated group.</param>
        /// <returns>The pattern.</returns>
        public static GroupPattern Classify(GroupEvaluation group)
        {
            ArgumentNullException.ThrowIfNull(group);
            switch (group.DecisionRule)
            {
                case DecisionRule.ConfirmedPaid:
                    return ClassifyConfirmedPaid(group);
                case DecisionRule.NoOwnerRetire:
                    if (group.Members.All(member => member.DeletionBasis == DeletionBasis.SupersededByReplacement))
                    {
                        return GroupPattern.SupersededAttempts;
                    }

                    return group.Members.All(member => member.DeletionBasis == DeletionBasis.AbandonedOrphan)
                        ? GroupPattern.AbandonedOrphanAttempts
                        : GroupPattern.UnpaidAttempts;
                case DecisionRule.ReviewConfirmedOwner:
                    return GroupPattern.ConfirmedOwnerNotPaid;
                case DecisionRule.MultiConfirmed:
                    return ClassifyMultiConfirmed(group);
                case DecisionRule.ReviewUnconfirmedPaidDonation:
                    return GroupPattern.UnconfirmedPaidDonation;
                case DecisionRule.ReviewUnconfirmedCapture:
                    return GroupPattern.UnconfirmedCapture;
                default:
                    return GroupPattern.Unclassified;
            }
        }

        /// <summary>
        /// Gets a short title for a pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>The title.</returns>
        public static string Title(GroupPattern pattern)
        {
            return pattern switch
            {
                GroupPattern.ReconciliationToolCopy => "Reconciliation tool copy",
                GroupPattern.ConfirmedPaymentWithZeroValueRetries => "Confirmed payment with zero-value retries",
                GroupPattern.ConfirmedPaymentWithUnlinkedCaptures => "Confirmed payment with unlinked captures",
                GroupPattern.SupersededAttempts => "Superseded attempts",
                GroupPattern.AbandonedOrphanAttempts => "Abandoned orphan attempts",
                GroupPattern.UnpaidAttempts => "Unpaid attempts",
                GroupPattern.ConfirmedOwnerNotPaid => "Confirmed owner without paid evidence",
                GroupPattern.SimultaneousSubscriptionCharges => "Simultaneous subscription charges",
                GroupPattern.RecurringChargesSharingId => "Recurring charges sharing one ID",
                GroupPattern.MultipleConfirmedOwners => "Multiple confirmed owners",
                GroupPattern.UnconfirmedPaidDonation => "Paid donation without confirmed payment",
                GroupPattern.UnconfirmedCapture => "Unconfirmed capture",
                _ => "Unclassified",
            };
        }

        /// <summary>
        /// Gets the general explanation of a pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>The explanation.</returns>
        public static string Explanation(GroupPattern pattern)
        {
            return pattern switch
            {
                GroupPattern.ReconciliationToolCopy =>
                    "The waiting-subscription reconciliation tool created a new completed payment row for a subscription donation and confirmed the donation with it. It did not check whether the provider ID was already stored, so the original webhook capture rows, which have no donation link, kept the same ID.",
                GroupPattern.ConfirmedPaymentWithZeroValueRetries =>
                    "One payment is confirmed by a Payed donation with a matching amount. The other rows are retries or notifications with the same provider ID that never completed, have no paid amount, and confirm no donation.",
                GroupPattern.ConfirmedPaymentWithUnlinkedCaptures =>
                    "One payment is confirmed by a Payed donation with a matching amount. Other rows with the same provider ID also carry capture evidence (paid amount or completion) without any confirmation.",
                GroupPattern.SupersededAttempts =>
                    "Every row is an unpaid, uncompleted, unconfirmed attempt whose donation was later paid by a different successful payment outside the group.",
                GroupPattern.AbandonedOrphanAttempts =>
                    "Every row is an unpaid, uncompleted, unconfirmed attempt without a donation link or successful status.",
                GroupPattern.UnpaidAttempts =>
                    "No row is confirmed, completed, paid, or successful by its own evidence, but not every row has a safe deletion basis.",
                GroupPattern.ConfirmedOwnerNotPaid =>
                    "Exactly one row is confirmed by a donation, but the paid checks fail: the donation is not Payed, the status is not successful, or the paid amount does not match.",
                GroupPattern.SimultaneousSubscriptionCharges =>
                    "Several rows were created in the same second with the same transaction key, and each confirms a different donation. Typically one subscription notification created several donations.",
                GroupPattern.RecurringChargesSharingId =>
                    "Charges on different dates reused one provider ID and each confirms a different donation. The provider may have reported the same capture ID for recurring charges, or the app copied it.",
                GroupPattern.MultipleConfirmedOwners =>
                    "More than one row is confirmed by a donation. Only one row can keep the provider ID.",
                GroupPattern.UnconfirmedPaidDonation =>
                    "A row is linked to a Payed donation that has no confirmed payment, so this group may contain the payment that paid it.",
                GroupPattern.UnconfirmedCapture =>
                    "Rows carry capture evidence (completion, paid amount, or successful status) but no row is confirmed by a donation.",
                _ => "The group does not match a known pattern.",
            };
        }

        private static GroupPattern ClassifyConfirmedPaid(GroupEvaluation group)
        {
            MemberEvaluation? owner = group.FindMember(group.SuggestedCanonicalPaymentId);
            if (owner == null)
            {
                return GroupPattern.ConfirmedPaymentWithUnlinkedCaptures;
            }

            var others = group.Members.Where(member => member.PaymentId != owner.PaymentId).ToList();
            bool ownerCreatedCompleted = owner.Input.Completed.HasValue
                && owner.Input.Completed.Value >= owner.Input.Created
                && owner.Input.Completed.Value - owner.Input.Created <= ToolCreatedCompletedTolerance;
            if (owner.PaymentId == group.Members.Max(member => member.PaymentId)
                && owner.Input.DonationId.HasValue
                && ownerCreatedCompleted
                && others.All(member => member.MissingDonationLink && member.ConfirmationCount == 0 && member.IsPaid))
            {
                return GroupPattern.ReconciliationToolCopy;
            }

            if (others.All(member => !member.Input.Completed.HasValue
                && member.Input.Paid.HasValue && member.Input.Paid.Value == 0f
                && member.ConfirmationCount == 0))
            {
                return GroupPattern.ConfirmedPaymentWithZeroValueRetries;
            }

            return GroupPattern.ConfirmedPaymentWithUnlinkedCaptures;
        }

        private static GroupPattern ClassifyMultiConfirmed(GroupEvaluation group)
        {
            DateTime first = group.Members.Min(member => member.Input.Created);
            DateTime last = group.Members.Max(member => member.Input.Created);
            if (group.SameTransactionKey && last - first <= SimultaneousTolerance)
            {
                return GroupPattern.SimultaneousSubscriptionCharges;
            }

            int distinctDays = group.Members.Select(member => member.Input.Created.Date).Distinct().Count();
            return distinctDays > 1 ? GroupPattern.RecurringChargesSharingId : GroupPattern.MultipleConfirmedOwners;
        }
    }
}
