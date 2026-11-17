// -----------------------------------------------------------------------
// <copyright file="GroupEvaluation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Group facts and classification computed by the SQL planner (<c>#GroupPlan</c>).
    /// </summary>
    public sealed record GroupEvaluation
    {
        /// <summary>
        /// Gets the group identifier: the lowest member payment ID.
        /// </summary>
        public int GroupId { get; init; }

        /// <summary>
        /// Gets the provider identifier from the current-rank member.
        /// </summary>
        public string EasyPayPaymentId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the evaluated members ordered by payment ID.
        /// </summary>
        public IReadOnlyList<MemberEvaluation> Members { get; init; } = new List<MemberEvaluation>();

        /// <summary>
        /// Gets the member count.
        /// </summary>
        public int PaymentCount => this.Members.Count;

        /// <summary>
        /// Gets the number of members without a forward donation link.
        /// </summary>
        public int OrphanCount { get; init; }

        /// <summary>
        /// Gets the number of deletion candidates.
        /// </summary>
        public int DeletionCandidateCount { get; init; }

        /// <summary>
        /// Gets the number of members whose Payed donation is confirmed outside the group.
        /// </summary>
        public int ExternalConfirmationCandidateCount { get; init; }

        /// <summary>
        /// Gets the number of deletion candidates protected by financial evidence.
        /// </summary>
        public int DeletionFinancialConflictCount { get; init; }

        /// <summary>
        /// Gets the number of deletion candidates protected by unknown status.
        /// </summary>
        public int DeletionUnknownStatusCount { get; init; }

        /// <summary>
        /// Gets the number of completed members.
        /// </summary>
        public int CompletedCount { get; init; }

        /// <summary>
        /// Gets the number of members with a successful status.
        /// </summary>
        public int SuccessfulStatusCount { get; init; }

        /// <summary>
        /// Gets the number of members with their own success evidence.
        /// </summary>
        public int OwnSuccessCount { get; init; }

        /// <summary>
        /// Gets the number of members with a known unsuccessful status.
        /// </summary>
        public int NonSuccessfulStatusCount { get; init; }

        /// <summary>
        /// Gets the number of members with unknown status.
        /// </summary>
        public int UnknownStatusCount { get; init; }

        /// <summary>
        /// Gets the number of unsuccessful members that still carry financial evidence.
        /// </summary>
        public int StatusConflictCount { get; init; }

        /// <summary>
        /// Gets the number of members with financial evidence.
        /// </summary>
        public int FinancialEvidenceCount { get; init; }

        /// <summary>
        /// Gets the number of members with a safe deletion basis.
        /// </summary>
        public int DeletableAttemptCount { get; init; }

        /// <summary>
        /// Gets the number of members confirmed by at least one donation.
        /// </summary>
        public int ConfirmedOwnerCount { get; init; }

        /// <summary>
        /// Gets the number of members linked to a Payed donation without a confirmed payment.
        /// </summary>
        public int UnconfirmedPaidDonationLinkCount { get; init; }

        /// <summary>
        /// Gets the number of members with a positive paid amount.
        /// </summary>
        public int PaidMemberCount { get; init; }

        /// <summary>
        /// Gets whether all members share one non-empty transaction key.
        /// </summary>
        public bool SameTransactionKey { get; init; }

        /// <summary>
        /// Gets whether all members share the same requested and paid amounts.
        /// </summary>
        public bool SameAmounts { get; init; }

        /// <summary>
        /// Gets the current-rank payment identifier.
        /// </summary>
        public int CurrentRankPaymentId { get; init; }

        /// <summary>
        /// Gets the lowest confirmed member identifier.
        /// </summary>
        public int? ConfirmedCandidatePaymentId { get; init; }

        /// <summary>
        /// Gets the decision rule.
        /// </summary>
        public DecisionRule DecisionRule { get; init; }

        /// <summary>
        /// Gets the planner's suggested owner.
        /// </summary>
        public int? SuggestedCanonicalPaymentId { get; init; }

        /// <summary>
        /// Gets whether every member is a deletion candidate.
        /// </summary>
        public bool DeletionCandidate { get; init; }

        /// <summary>
        /// Gets whether any deletion candidate is protected by financial evidence.
        /// </summary>
        public bool DeletionBlockedByFinancialEvidence { get; init; }

        /// <summary>
        /// Gets whether every member has a safe deletion basis.
        /// </summary>
        public bool HasSafeDeletionEvidence { get; init; }

        /// <summary>
        /// Gets the planner's suggested action.
        /// </summary>
        public SuggestedAction SuggestedAction { get; init; }

        /// <summary>
        /// Gets the action the SQL planner prefills, when it prefills one.
        /// </summary>
        public ReconciliationAction? PlannerAction { get; init; }

        /// <summary>
        /// Gets a member by payment ID.
        /// </summary>
        /// <param name="paymentId">Payment identifier.</param>
        /// <returns>The member, or null.</returns>
        public MemberEvaluation? FindMember(int? paymentId)
        {
            return paymentId.HasValue ? this.Members.FirstOrDefault(member => member.PaymentId == paymentId.Value) : null;
        }
    }
}
