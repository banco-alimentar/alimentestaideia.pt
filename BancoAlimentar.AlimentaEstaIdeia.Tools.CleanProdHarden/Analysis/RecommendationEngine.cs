// -----------------------------------------------------------------------
// <copyright file="RecommendationEngine.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Builds an explanation and a recommended decision for a duplicate group.
    /// </summary>
    public static class RecommendationEngine
    {
        /// <summary>
        /// Builds a recommendation.
        /// </summary>
        /// <param name="group">Evaluated group.</param>
        /// <param name="context">Extra evidence.</param>
        /// <returns>The recommendation.</returns>
        public static GroupRecommendation Recommend(GroupEvaluation group, GroupContext context)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(context);

            GroupPattern pattern = GroupPatternClassifier.Classify(group);
            var rationale = new List<string>();
            var followUps = new List<string>();
            ReconciliationAction? action = null;
            int? owner = null;
            RecommendationConfidence confidence = RecommendationConfidence.None;

            switch (group.DecisionRule)
            {
                case DecisionRule.ConfirmedPaid:
                    action = ReconciliationAction.ReconcileIdentifiers;
                    owner = group.SuggestedCanonicalPaymentId;
                    confidence = pattern == GroupPattern.ConfirmedPaymentWithUnlinkedCaptures ? RecommendationConfidence.Medium : RecommendationConfidence.High;
                    rationale.Add(string.Format(CultureInfo.InvariantCulture, "Payment {0} is the only member confirmed by a Payed donation, its status is successful, and its paid amount matches the donation (SQL rule ConfirmedPaid).", owner));
                    rationale.Add("RetireIdentifier is not allowed because a member is confirmed by a donation (51013), and DeleteGroup is not allowed because the owner has financial evidence (51011).");
                    if (pattern == GroupPattern.ReconciliationToolCopy)
                    {
                        rationale.Add("The owner is the newest row, was inserted already completed, and is the only row linked to the donation. The older rows are the original captures without a donation link; they keep their amounts and only lose the provider ID.");
                    }
                    else if (pattern == GroupPattern.ConfirmedPaymentWithUnlinkedCaptures)
                    {
                        followUps.Add(string.Format(CultureInfo.InvariantCulture, "Other members carry capture evidence. Confirm in the EasyPay backoffice that provider ID {0} has exactly one capture.", group.EasyPayPaymentId));
                    }

                    AddOtherDonationFollowUps(group, owner, followUps);
                    break;

                case DecisionRule.NoOwnerRetire:
                    action = ReconciliationAction.RetireIdentifier;
                    confidence = RecommendationConfidence.High;
                    rationale.Add("No member is confirmed, completed, paid, or successful by its own evidence, and no linked Payed donation lacks a confirmed payment (SQL rule NoOwnerRetire).");
                    rationale.Add("The workflow documentation recommends RetireIdentifier unless deletion is specifically required: it keeps every row and only clears the provider ID, with an audit record.");
                    if (group.HasSafeDeletionEvidence)
                    {
                        rationale.Add(context.CanDeleteGroup == false
                            ? "Every member has a safe deletion basis, but DeleteGroup is blocked by dependent rows, permissions, or triggers."
                            : "Every member has a safe deletion basis, so DeleteGroup is an allowed alternative if the rows must be removed.");
                    }

                    rationale.Add("A late provider notification is matched by transaction key and can write the provider ID back to a retired row. Re-check readiness right before running the migration.");
                    break;

                case DecisionRule.ReviewConfirmedOwner:
                    action = ReconciliationAction.ReconcileIdentifiers;
                    owner = group.ConfirmedCandidatePaymentId;
                    confidence = owner == group.SuggestedCanonicalPaymentId ? RecommendationConfidence.Medium : RecommendationConfidence.Low;
                    rationale.Add(string.Format(CultureInfo.InvariantCulture, "Payment {0} is the only member confirmed by a donation, but the ConfirmedPaid checks fail.", owner));
                    rationale.Add("RetireIdentifier is not allowed while a member is confirmed (51013). Keeping the ID on the confirmed member preserves the existing donation relationship.");
                    if (group.SuggestedCanonicalPaymentId.HasValue && group.SuggestedCanonicalPaymentId != owner)
                    {
                        rationale.Add(string.Format(CultureInfo.InvariantCulture, "The SQL planner suggests payment {0} because it is the only member with a successful status. Decide which row represents the real capture.", group.SuggestedCanonicalPaymentId));
                    }

                    followUps.Add(string.Format(CultureInfo.InvariantCulture, "Check provider ID {0} in the EasyPay backoffice. If it was captured, the confirmed donation's payment status needs a separate correction; if it was not, no money is affected.", group.EasyPayPaymentId));
                    AddOtherDonationFollowUps(group, owner, followUps);
                    break;

                case DecisionRule.MultiConfirmed:
                    action = ReconciliationAction.ReconcileIdentifiers;
                    owner = ChooseMultiConfirmedOwner(group, context, rationale);
                    confidence = RecommendationConfidence.Low;
                    rationale.Insert(0, string.Format(CultureInfo.InvariantCulture, "{0} members are each confirmed by a different donation (SQL rule MultiConfirmed). Only one can keep the provider ID; the others keep their donations but lose the ID.", group.ConfirmedOwnerCount));
                    AddMultiConfirmedFollowUps(group, owner, context, followUps);
                    break;

                case DecisionRule.ReviewUnconfirmedPaidDonation:
                    List<MemberEvaluation> linked = group.Members.Where(member => member.LinkedToUnconfirmedPaidDonation).ToList();
                    if (linked.Count == 1)
                    {
                        action = ReconciliationAction.ReconcileIdentifiers;
                        owner = linked[0].PaymentId;
                        confidence = RecommendationConfidence.Low;
                        rationale.Add(string.Format(CultureInfo.InvariantCulture, "Payment {0} is linked to Payed donation {1}, which has no confirmed payment; this payment may have paid it.", owner, linked[0].Input.DonationId));
                    }
                    else
                    {
                        rationale.Add("Several members are linked to Payed donations without a confirmed payment. Decide which row represents the real capture.");
                    }

                    followUps.Add("Set ConfirmedPaymentId on the Payed donation outside this tool after confirming which payment paid it.");
                    break;

                default:
                    owner = group.SuggestedCanonicalPaymentId ?? SingleCaptureOwner(group);
                    if (owner.HasValue)
                    {
                        action = ReconciliationAction.ReconcileIdentifiers;
                        confidence = RecommendationConfidence.Low;
                        rationale.Add(string.Format(CultureInfo.InvariantCulture, "No member is confirmed by a donation, but payment {0} is the only member with capture evidence.", owner));
                    }
                    else
                    {
                        rationale.Add("Several members carry capture evidence and none is confirmed by a donation. Check the provider before choosing an owner, or retire the ID if none of them paid a donation.");
                    }

                    followUps.Add(string.Format(CultureInfo.InvariantCulture, "Check provider ID {0} in the EasyPay backoffice to identify which row captured money and whether a donation is missing its payment.", group.EasyPayPaymentId));
                    break;
            }

            List<ReconciliationAction> alternatives = AllowedActions(group, context).Where(candidate => candidate != action).ToList();
            return new GroupRecommendation
            {
                Pattern = pattern,
                Narrative = BuildNarrative(group, pattern),
                Action = action,
                CanonicalPaymentId = action == ReconciliationAction.ReconcileIdentifiers ? owner : null,
                Confidence = confidence,
                Rationale = rationale,
                SuggestedReason = SuggestReason(group, pattern, action, owner),
                Alternatives = alternatives,
                FollowUps = followUps,
            };
        }

        /// <summary>
        /// Gets the actions that the group's evidence allows, before environment checks.
        /// </summary>
        /// <param name="group">Evaluated group.</param>
        /// <param name="context">Extra evidence.</param>
        /// <returns>The allowed actions.</returns>
        public static IReadOnlyList<ReconciliationAction> AllowedActions(GroupEvaluation group, GroupContext context)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(context);
            var actions = new List<ReconciliationAction> { ReconciliationAction.ReconcileIdentifiers };
            if (group.ConfirmedOwnerCount == 0)
            {
                actions.Add(ReconciliationAction.RetireIdentifier);
            }

            if (group.HasSafeDeletionEvidence && context.CanDeleteGroup != false)
            {
                actions.Add(ReconciliationAction.DeleteGroup);
            }

            return actions;
        }

        /// <summary>
        /// Formats an amount as the UI and reasons show it.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <returns>The formatted amount.</returns>
        public static string FormatAmount(double? amount)
        {
            return amount.HasValue ? DuplicateGroupRules.ToDecimal4(amount.Value).ToString("0.00##", CultureInfo.InvariantCulture) : "null";
        }

        /// <summary>
        /// Builds the default audit reason for an action the operator selected.
        /// </summary>
        /// <param name="group">Evaluated group.</param>
        /// <param name="pattern">Classified pattern.</param>
        /// <param name="action">Selected action.</param>
        /// <param name="owner">Selected owner for ReconcileIdentifiers.</param>
        /// <returns>The suggested reason, or an empty string when the selection is incomplete.</returns>
        public static string SuggestReason(GroupEvaluation group, GroupPattern pattern, ReconciliationAction? action, int? owner)
        {
            ArgumentNullException.ThrowIfNull(group);
            string title = GroupPatternClassifier.Title(pattern);
            return action switch
            {
                ReconciliationAction.ReconcileIdentifiers when owner.HasValue => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1}): keep provider ID on payment {2}; clear it with audit on the other {3} member(s).",
                    title,
                    group.DecisionRule,
                    owner,
                    group.PaymentCount - 1),
                ReconciliationAction.RetireIdentifier => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1}): no member owns the provider ID; retire it with audit on all {2} members.",
                    title,
                    group.DecisionRule,
                    group.PaymentCount),
                ReconciliationAction.DeleteGroup => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1}): no member owns the provider ID; delete all {2} unpaid attempts after archiving each row as JSON.",
                    title,
                    group.DecisionRule,
                    group.PaymentCount),
                _ => string.Empty,
            };
        }

        private static int? ChooseMultiConfirmedOwner(GroupEvaluation group, GroupContext context, List<string> rationale)
        {
            List<MemberEvaluation> confirmed = group.Members.Where(member => member.ConfirmationCount > 0).ToList();
            List<MemberEvaluation> ownSuccess = confirmed
                .Where(member => member.StatusSource == StatusSource.Payment && member.IsSuccessfulStatus == true)
                .ToList();
            if (ownSuccess.Count == 1)
            {
                rationale.Add(string.Format(CultureInfo.InvariantCulture, "Payment {0} is the only confirmed member whose own provider status is successful.", ownSuccess[0].PaymentId));
                return ownSuccess[0].PaymentId;
            }

            List<MemberEvaluation> invoiced = confirmed
                .Where(member => member.Input.ConfirmingDonations.Any(donation => context.DonationIdsWithInvoice.Contains(donation.Id)))
                .ToList();
            if (invoiced.Count == 1)
            {
                rationale.Add(string.Format(CultureInfo.InvariantCulture, "Payment {0} is the only confirmed member whose donation has an invoice.", invoiced[0].PaymentId));
                return invoiced[0].PaymentId;
            }

            rationale.Add("No status or invoice evidence identifies a single owner, so the lowest confirmed payment is suggested. Pick the row that matches the provider's capture.");
            return group.ConfirmedCandidatePaymentId;
        }

        private static int? SingleCaptureOwner(GroupEvaluation group)
        {
            List<MemberEvaluation> captures = group.Members
                .Where(member => member.Input.Completed.HasValue || member.IsPaid || member.IsOwnSuccess)
                .ToList();
            return captures.Count == 1 ? captures[0].PaymentId : null;
        }

        private static void AddOtherDonationFollowUps(GroupEvaluation group, int? owner, List<string> followUps)
        {
            MemberEvaluation? ownerMember = group.FindMember(owner);
            HashSet<int> ownerDonations = ownerMember?.Input.ConfirmingDonations.Select(donation => donation.Id).ToHashSet() ?? new HashSet<int>();
            if (ownerMember?.Input.DonationId is int ownerForward)
            {
                ownerDonations.Add(ownerForward);
            }

            foreach (MemberEvaluation member in group.Members.Where(member => member.PaymentId != owner))
            {
                DonationFacts? donation = member.Input.ForwardDonation;
                if (donation != null && !ownerDonations.Contains(donation.Id) && donation.PaymentStatus != PaymentStatus.Payed)
                {
                    followUps.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Payment {0} is linked to donation {1} ({2}, {3}). It is a different donation from the owner's; check whether it is a duplicate subscription donation that should be cancelled.",
                        member.PaymentId,
                        donation.Id,
                        donation.PaymentStatus,
                        FormatAmount(donation.DonationAmount)));
                }
            }
        }

        private static void AddMultiConfirmedFollowUps(GroupEvaluation group, int? owner, GroupContext context, List<string> followUps)
        {
            followUps.Add(string.Format(CultureInfo.InvariantCulture, "Verify in the EasyPay backoffice how many captures exist for provider ID {0}. If only one, the other confirmed donations are duplicates; if several, the provider reused the ID.", group.EasyPayPaymentId));
            foreach (MemberEvaluation member in group.Members.Where(member => member.ConfirmationCount > 0 && member.PaymentId != owner))
            {
                foreach (DonationFacts donation in member.Input.ConfirmingDonations)
                {
                    bool invoiced = context.DonationIdsWithInvoice.Contains(donation.Id);
                    string subscription = context.SubscriptionIdByDonationId.TryGetValue(donation.Id, out int subscriptionId)
                        ? string.Format(CultureInfo.InvariantCulture, ", subscription {0}", subscriptionId)
                        : string.Empty;
                    followUps.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Donation {0} ({1}, {2}{3}) stays confirmed by payment {4}{5}. Resolve it as a duplicate donation separately if the provider shows one capture.",
                        donation.Id,
                        donation.PaymentStatus,
                        FormatAmount(donation.DonationAmount),
                        subscription,
                        member.PaymentId,
                        invoiced ? " and has an invoice" : string.Empty));
                }
            }

            followUps.Add("The Tools command reconcile-waiting-subscription-donations --cleanup-duplicates hard-deletes duplicate donations. Review its dry run before using it.");
        }

        private static string BuildNarrative(GroupEvaluation group, GroupPattern pattern)
        {
            DateTime first = group.Members.Min(member => member.Input.Created);
            DateTime last = group.Members.Max(member => member.Input.Created);
            string types = string.Join(", ", group.Members.Select(member => member.Input.PaymentType).Distinct());
            string header = string.Format(
                CultureInfo.InvariantCulture,
                "{0} payments ({1}) share provider ID {2}, created between {3:yyyy-MM-dd HH:mm:ss} and {4:yyyy-MM-dd HH:mm:ss}. ",
                group.PaymentCount,
                types,
                group.EasyPayPaymentId,
                first,
                last);
            return header + GroupPatternClassifier.Explanation(pattern);
        }
    }
}
