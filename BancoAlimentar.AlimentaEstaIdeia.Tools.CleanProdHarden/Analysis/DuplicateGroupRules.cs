// -----------------------------------------------------------------------
// <copyright file="DuplicateGroupRules.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    /// <summary>
    /// Port of the planning rules in <c>MergeDuplicateEasyPayPaymentIds.sql</c>.
    /// SQL three-valued logic is kept: an unknown comparison never satisfies a <c>CASE WHEN</c>.
    /// </summary>
    public static class DuplicateGroupRules
    {
        /// <summary>
        /// The PayPal discriminator value.
        /// </summary>
        public const string PayPalPaymentType = "PayPalPayment";

        /// <summary>
        /// Status values the SQL script treats as successful, after trimming and lower-casing.
        /// </summary>
        public static readonly IReadOnlyList<string> SqlSuccessfulStatuses = new[] { "paid", "success", "ok", "approved", "completed" };

        /// <summary>
        /// Gets a comparer that matches the database collation for provider IDs: case-insensitive, trailing spaces ignored.
        /// </summary>
        public static StringComparer ProviderIdComparer { get; } = new TrailingSpaceInsensitiveComparer();

        /// <summary>
        /// Returns true when a non-null status is successful, using the application's shared status rule.
        /// </summary>
        /// <param name="status">Provider status.</param>
        /// <returns>True for a successful status.</returns>
        public static bool IsSuccessfulStatusText(string? status)
        {
            return status != null && DonationPaymentCompletion.IsSuccessfulPaymentStatus(status.Trim(' '));
        }

        /// <summary>
        /// Compares a real paid amount with a donation amount as <c>CONVERT(decimal(19,4), ...)</c> does.
        /// </summary>
        /// <param name="paid">Paid amount.</param>
        /// <param name="amount">Donation amount.</param>
        /// <returns>True when both are present and equal at four decimal places.</returns>
        public static bool AmountsMatch(float? paid, double? amount)
        {
            if (!paid.HasValue || !amount.HasValue || !double.IsFinite(paid.Value) || !double.IsFinite(amount.Value))
            {
                return false;
            }

            return ToDecimal4(paid.Value) == ToDecimal4(amount.Value);
        }

        /// <summary>
        /// Converts a floating-point value to <c>decimal(19,4)</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The rounded decimal value.</returns>
        public static decimal ToDecimal4(double value)
        {
            return Math.Round((decimal)value, 4, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Groups members by provider ID with the database collation and evaluates every group with more than one member.
        /// </summary>
        /// <param name="members">Payments that carry a provider ID.</param>
        /// <returns>Evaluated groups ordered by group ID.</returns>
        public static IReadOnlyList<GroupEvaluation> EvaluateGroups(IEnumerable<PaymentMemberInput> members)
        {
            ArgumentNullException.ThrowIfNull(members);
            return members
                .GroupBy(member => member.EasyPayPaymentId, ProviderIdComparer)
                .Where(group => group.Count() > 1)
                .Select(EvaluateGroup)
                .OrderBy(group => group.GroupId)
                .ToList();
        }

        /// <summary>
        /// Evaluates one duplicate group.
        /// </summary>
        /// <param name="groupMembers">All payments that share the provider ID.</param>
        /// <returns>The evaluated group.</returns>
        public static GroupEvaluation EvaluateGroup(IEnumerable<PaymentMemberInput> groupMembers)
        {
            ArgumentNullException.ThrowIfNull(groupMembers);
            List<PaymentMemberInput> inputs = groupMembers.OrderBy(member => member.PaymentId).ToList();
            if (inputs.Count == 0)
            {
                throw new ArgumentException("A group needs at least one member.", nameof(groupMembers));
            }

            HashSet<int> memberIds = inputs.Select(member => member.PaymentId).ToHashSet();
            Dictionary<int, int> ranks = inputs
                .OrderBy(member => member.Completed.HasValue ? 0 : 1)
                .ThenByDescending(member => member.Created)
                .ThenByDescending(member => member.PaymentId)
                .Select((member, index) => (member.PaymentId, Rank: index + 1))
                .ToDictionary(item => item.PaymentId, item => item.Rank);

            List<MemberEvaluation> members = inputs
                .Select(input => EvaluateMember(input, memberIds, ranks[input.PaymentId]))
                .ToList();

            MemberEvaluation currentRank = members.Single(member => member.CurrentRank == 1);
            MemberEvaluation? candidate = members.Where(member => member.ConfirmationCount > 0).OrderBy(member => member.PaymentId).FirstOrDefault();

            int confirmedOwnerCount = members.Count(member => member.ConfirmationCount > 0);
            int unconfirmedPaidDonationLinkCount = members.Count(member => member.LinkedToUnconfirmedPaidDonation);
            int ownSuccessCount = members.Count(member => member.IsOwnSuccess);
            int completedCount = members.Count(member => member.Input.Completed.HasValue);
            int paidMemberCount = members.Count(member => member.IsPaid);
            int successfulStatusCount = members.Count(member => member.IsSuccessfulStatus == true);

            DecisionRule rule = DecideRule(candidate, confirmedOwnerCount, unconfirmedPaidDonationLinkCount, ownSuccessCount, completedCount, paidMemberCount);
            int? suggestedCanonical = rule switch
            {
                DecisionRule.ConfirmedPaid => candidate?.PaymentId,
                DecisionRule.MultiConfirmed or DecisionRule.NoOwnerRetire => null,
                _ when successfulStatusCount == 1 => members.Where(member => member.IsSuccessfulStatus == true).Min(member => member.PaymentId),
                DecisionRule.ReviewConfirmedOwner => candidate?.PaymentId,
                _ => null,
            };

            int deletionCandidateCount = members.Count(member => member.DeletionCandidate);
            int deletableAttemptCount = members.Count(member => member.DeletionBasis.HasValue);

            return new GroupEvaluation
            {
                GroupId = members[0].PaymentId,
                EasyPayPaymentId = currentRank.Input.EasyPayPaymentId,
                Members = members,
                OrphanCount = members.Count(member => member.MissingDonationLink),
                DeletionCandidateCount = deletionCandidateCount,
                ExternalConfirmationCandidateCount = members.Count(member => member.HasExternalDonationConfirmation),
                DeletionFinancialConflictCount = members.Count(member => member.DeletionBlockedByFinancialEvidence),
                DeletionUnknownStatusCount = members.Count(member => member.DeletionBlockedByUnknownStatus),
                CompletedCount = completedCount,
                SuccessfulStatusCount = successfulStatusCount,
                OwnSuccessCount = ownSuccessCount,
                NonSuccessfulStatusCount = members.Count(member => member.IsSuccessfulStatus == false),
                UnknownStatusCount = members.Count(member => !member.IsSuccessfulStatus.HasValue),
                StatusConflictCount = members.Count(member => member.IsSuccessfulStatus == false && member.HasFinancialEvidence),
                FinancialEvidenceCount = members.Count(member => member.HasFinancialEvidence),
                DeletableAttemptCount = deletableAttemptCount,
                ConfirmedOwnerCount = confirmedOwnerCount,
                UnconfirmedPaidDonationLinkCount = unconfirmedPaidDonationLinkCount,
                PaidMemberCount = paidMemberCount,
                SameTransactionKey = HasSameTransactionKey(inputs),
                SameAmounts = HasSameAmounts(inputs),
                CurrentRankPaymentId = currentRank.PaymentId,
                ConfirmedCandidatePaymentId = candidate?.PaymentId,
                DecisionRule = rule,
                SuggestedCanonicalPaymentId = suggestedCanonical,
                DeletionCandidate = deletionCandidateCount == members.Count,
                DeletionBlockedByFinancialEvidence = members.Any(member => member.DeletionBlockedByFinancialEvidence),
                HasSafeDeletionEvidence = deletableAttemptCount == members.Count,
                SuggestedAction = rule switch
                {
                    DecisionRule.ConfirmedPaid => SuggestedAction.ReconcileIdentifiers,
                    DecisionRule.NoOwnerRetire => SuggestedAction.RetireIdentifier,
                    DecisionRule.MultiConfirmed => SuggestedAction.ResolveDuplicateDonations,
                    _ => SuggestedAction.Review,
                },
                PlannerAction = rule switch
                {
                    DecisionRule.ConfirmedPaid => ReconciliationAction.ReconcileIdentifiers,
                    DecisionRule.NoOwnerRetire => ReconciliationAction.RetireIdentifier,
                    _ => null,
                },
            };
        }

        private static MemberEvaluation EvaluateMember(PaymentMemberInput input, IReadOnlySet<int> memberIds, int rank)
        {
            DonationFacts? forward = input.DonationId.HasValue && input.ForwardDonation?.Id == input.DonationId
                ? input.ForwardDonation
                : null;
            IReadOnlyList<DonationFacts> confirmations = input.ConfirmingDonations;
            int confirmationCount = confirmations.Count;
            int? confirmedDonationId = confirmationCount > 0 ? confirmations.Min(donation => donation.Id) : null;
            double? confirmedDonationAmount = confirmationCount > 0 ? confirmations.Min(donation => donation.DonationAmount) : null;
            PaymentStatus? confirmedDonationPaymentStatus = confirmationCount > 0 ? confirmations.Min(donation => donation.PaymentStatus) : null;

            int? statusDonationId = forward != null ? forward.Id : confirmationCount == 1 ? confirmedDonationId : null;
            PaymentStatus? donationPaymentStatus = forward != null ? forward.PaymentStatus : confirmationCount == 1 ? confirmedDonationPaymentStatus : null;

            bool forwardPayed = forward?.PaymentStatus == PaymentStatus.Payed;
            bool hasExternalDonationConfirmation = forwardPayed
                && forward!.ConfirmedPaymentId.HasValue
                && !memberIds.Contains(forward.ConfirmedPaymentId.Value);
            bool linkedToUnconfirmedPaidDonation = forwardPayed && !forward!.ConfirmedPaymentId.HasValue;
            bool missingDonationLink = !input.DonationId.HasValue;
            bool relationshipDeletionCandidate = missingDonationLink || hasExternalDonationConfirmation;
            bool hasSuccessfulReplacement = forwardPayed && IsSuccessfulReplacement(forward!, input.Replacement, memberIds);

            StatusSource statusSource = input.Status != null ? StatusSource.Payment
                : forward != null ? StatusSource.Donation
                : confirmationCount == 1 ? StatusSource.ConfirmedDonation
                : StatusSource.Unknown;

            bool? isSuccessfulStatus = input.Status != null ? IsSuccessfulStatusText(input.Status)
                : donationPaymentStatus == PaymentStatus.Payed ? true
                : donationPaymentStatus is PaymentStatus.NotPayed or PaymentStatus.WaitingPayment or PaymentStatus.ErrorPayment ? false
                : null;

            bool deletionCandidate = relationshipDeletionCandidate || isSuccessfulStatus == false;
            bool isOwnSuccess = isSuccessfulStatus == true && !(statusSource == StatusSource.Donation && hasExternalDonationConfirmation);
            bool hasFinancialEvidence = input.Completed.HasValue
                || input.Paid > 0
                || confirmationCount > 0
                || (donationPaymentStatus == PaymentStatus.Payed && !hasSuccessfulReplacement);

            // NOT (DonationId IS NULL AND ConfirmationCount = 0 AND Completed IS NULL AND Paid = 0) is unknown, and so false, when Paid is NULL.
            bool unknownStatusNotOrphanShape = !(missingDonationLink && confirmationCount == 0 && !input.Completed.HasValue)
                || (input.Paid.HasValue && input.Paid.Value != 0f);

            bool unpaidUncompletedUnconfirmed = !input.Completed.HasValue
                && input.Paid.HasValue && input.Paid.Value == 0f
                && confirmationCount == 0;
            DeletionBasis? deletionBasis = null;
            if (unpaidUncompletedUnconfirmed
                && hasSuccessfulReplacement
                && (isSuccessfulStatus == false
                    || (isSuccessfulStatus == true && statusSource == StatusSource.Donation && hasExternalDonationConfirmation)))
            {
                deletionBasis = DeletionBasis.SupersededByReplacement;
            }
            else if (unpaidUncompletedUnconfirmed && missingDonationLink && isSuccessfulStatus != true)
            {
                deletionBasis = DeletionBasis.AbandonedOrphan;
            }

            return new MemberEvaluation
            {
                Input = input,
                CurrentRank = rank,
                StatusSource = statusSource,
                StatusDonationId = statusDonationId,
                DonationPaymentStatus = donationPaymentStatus,
                IsSuccessfulStatus = isSuccessfulStatus,
                ConfirmationCount = confirmationCount,
                ConfirmedDonationId = confirmedDonationId,
                ConfirmedDonationAmount = confirmedDonationAmount,
                ConfirmedDonationPaymentStatus = confirmedDonationPaymentStatus,
                LinkedToUnconfirmedPaidDonation = linkedToUnconfirmedPaidDonation,
                MissingDonationLink = missingDonationLink,
                HasExternalDonationConfirmation = hasExternalDonationConfirmation,
                RelationshipDeletionCandidate = relationshipDeletionCandidate,
                DonationConfirmedPaymentId = forward?.ConfirmedPaymentId,
                HasSuccessfulReplacement = hasSuccessfulReplacement,
                DeletionCandidate = deletionCandidate,
                DeletionCandidateReason = missingDonationLink ? Analysis.DeletionCandidateReason.MissingDonationId
                    : hasExternalDonationConfirmation ? Analysis.DeletionCandidateReason.PaidDonationConfirmedOutsideGroup
                    : isSuccessfulStatus == false ? Analysis.DeletionCandidateReason.UnsuccessfulStatus
                    : null,
                DeletionBlockedByFinancialEvidence = deletionCandidate && (isOwnSuccess || hasFinancialEvidence),
                DeletionBlockedByUnknownStatus = deletionCandidate && !isSuccessfulStatus.HasValue && unknownStatusNotOrphanShape,
                DeletionBasis = deletionBasis,
                IsOwnSuccess = isOwnSuccess,
                HasFinancialEvidence = hasFinancialEvidence,
            };
        }

        private static bool IsSuccessfulReplacement(DonationFacts forward, ReplacementPaymentFacts? replacement, IReadOnlySet<int> memberIds)
        {
            if (replacement == null || replacement.Id != forward.ConfirmedPaymentId || memberIds.Contains(replacement.Id))
            {
                return false;
            }

            if (replacement.DonationId.HasValue && replacement.DonationId.Value != forward.Id)
            {
                return false;
            }

            bool easyPayEvidence = IsSuccessfulStatusText(replacement.Status)
                && replacement.Paid > 0
                && AmountsMatch(replacement.Paid, forward.DonationAmount);
            bool payPalEvidence = string.Equals(replacement.PaymentType, PayPalPaymentType, StringComparison.Ordinal)
                && string.Equals(replacement.Status?.Trim(' '), "completed", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(replacement.PayPalPaymentId?.Trim(' '))
                && !string.IsNullOrEmpty(replacement.PayerId?.Trim(' '));
            return easyPayEvidence || payPalEvidence;
        }

        private static DecisionRule DecideRule(
            MemberEvaluation? candidate,
            int confirmedOwnerCount,
            int unconfirmedPaidDonationLinkCount,
            int ownSuccessCount,
            int completedCount,
            int paidMemberCount)
        {
            if (confirmedOwnerCount > 1)
            {
                return DecisionRule.MultiConfirmed;
            }

            if (unconfirmedPaidDonationLinkCount > 0)
            {
                return DecisionRule.ReviewUnconfirmedPaidDonation;
            }

            if (confirmedOwnerCount == 1 && candidate != null && IsConfirmedPaidCandidate(candidate))
            {
                return DecisionRule.ConfirmedPaid;
            }

            if (confirmedOwnerCount == 1)
            {
                return DecisionRule.ReviewConfirmedOwner;
            }

            if (ownSuccessCount == 0 && completedCount == 0 && paidMemberCount == 0)
            {
                return DecisionRule.NoOwnerRetire;
            }

            return DecisionRule.ReviewUnconfirmedCapture;
        }

        private static bool IsConfirmedPaidCandidate(MemberEvaluation candidate)
        {
            return candidate.ConfirmationCount == 1
                && candidate.ConfirmedDonationPaymentStatus == PaymentStatus.Payed
                && (!candidate.Input.DonationId.HasValue || candidate.Input.DonationId == candidate.ConfirmedDonationId)
                && candidate.IsSuccessfulStatus == true
                && candidate.Input.Paid > 0
                && AmountsMatch(candidate.Input.Paid, candidate.ConfirmedDonationAmount);
        }

        private static bool HasSameTransactionKey(IReadOnlyCollection<PaymentMemberInput> inputs)
        {
            if (inputs.Any(member => string.IsNullOrEmpty(member.TransactionKey?.TrimEnd(' '))))
            {
                return false;
            }

            return inputs.Select(member => member.TransactionKey!).Distinct(ProviderIdComparer).Count() == 1;
        }

        private static bool HasSameAmounts(IReadOnlyCollection<PaymentMemberInput> inputs)
        {
            if (inputs.Any(member => !member.Requested.HasValue || !member.Paid.HasValue))
            {
                return false;
            }

            return inputs.Select(member => member.Requested!.Value).Distinct().Count() == 1
                && inputs.Select(member => member.Paid!.Value).Distinct().Count() == 1;
        }

        private sealed class TrailingSpaceInsensitiveComparer : StringComparer
        {
            public override int Compare(string? x, string? y)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x?.TrimEnd(' '), y?.TrimEnd(' '));
            }

            public override bool Equals(string? x, string? y)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(x?.TrimEnd(' '), y?.TrimEnd(' '));
            }

            public override int GetHashCode(string obj)
            {
                ArgumentNullException.ThrowIfNull(obj);
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TrimEnd(' '));
            }
        }
    }
}
