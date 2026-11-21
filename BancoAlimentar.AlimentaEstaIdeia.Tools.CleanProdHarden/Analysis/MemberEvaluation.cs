// -----------------------------------------------------------------------
// <copyright file="MemberEvaluation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Per-payment facts computed by the SQL planner (<c>#PaymentFacts</c> and member plan JSON).
    /// </summary>
    public sealed record MemberEvaluation
    {
        /// <summary>
        /// Gets the raw input.
        /// </summary>
        public required PaymentMemberInput Input { get; init; }

        /// <summary>
        /// Gets the payment identifier.
        /// </summary>
        public int PaymentId => this.Input.PaymentId;

        /// <summary>
        /// Gets the rank used by the planner: completed first, then newest, then highest ID.
        /// </summary>
        public int CurrentRank { get; init; }

        /// <summary>
        /// Gets the status evidence source.
        /// </summary>
        public StatusSource StatusSource { get; init; }

        /// <summary>
        /// Gets the donation used for status fallback.
        /// </summary>
        public int? StatusDonationId { get; init; }

        /// <summary>
        /// Gets the fallback donation payment status.
        /// </summary>
        public PaymentStatus? DonationPaymentStatus { get; init; }

        /// <summary>
        /// Gets whether the status is successful; null when unknown.
        /// </summary>
        public bool? IsSuccessfulStatus { get; init; }

        /// <summary>
        /// Gets the number of donations whose <c>ConfirmedPaymentId</c> is this payment.
        /// </summary>
        public int ConfirmationCount { get; init; }

        /// <summary>
        /// Gets the lowest confirming donation identifier.
        /// </summary>
        public int? ConfirmedDonationId { get; init; }

        /// <summary>
        /// Gets the lowest confirming donation amount.
        /// </summary>
        public double? ConfirmedDonationAmount { get; init; }

        /// <summary>
        /// Gets the lowest confirming donation payment status.
        /// </summary>
        public PaymentStatus? ConfirmedDonationPaymentStatus { get; init; }

        /// <summary>
        /// Gets whether the forward donation is Payed without a confirmed payment.
        /// </summary>
        public bool LinkedToUnconfirmedPaidDonation { get; init; }

        /// <summary>
        /// Gets whether <c>Payments.DonationId</c> is null.
        /// </summary>
        public bool MissingDonationLink { get; init; }

        /// <summary>
        /// Gets whether the forward donation is Payed and confirmed by a payment outside the group.
        /// </summary>
        public bool HasExternalDonationConfirmation { get; init; }

        /// <summary>
        /// Gets whether the member is a relationship deletion candidate.
        /// </summary>
        public bool RelationshipDeletionCandidate { get; init; }

        /// <summary>
        /// Gets the forward donation's confirmed payment identifier.
        /// </summary>
        public int? DonationConfirmedPaymentId { get; init; }

        /// <summary>
        /// Gets whether the forward donation is paid by a successful payment outside the group.
        /// </summary>
        public bool HasSuccessfulReplacement { get; init; }

        /// <summary>
        /// Gets whether the planner lists the member as a deletion candidate.
        /// </summary>
        public bool DeletionCandidate { get; init; }

        /// <summary>
        /// Gets the planner's deletion candidate reason.
        /// </summary>
        public DeletionCandidateReason? DeletionCandidateReason { get; init; }

        /// <summary>
        /// Gets whether a deletion candidate is protected by financial evidence.
        /// </summary>
        public bool DeletionBlockedByFinancialEvidence { get; init; }

        /// <summary>
        /// Gets whether a deletion candidate is protected by unknown status.
        /// </summary>
        public bool DeletionBlockedByUnknownStatus { get; init; }

        /// <summary>
        /// Gets the safe deletion basis, when there is one.
        /// </summary>
        public DeletionBasis? DeletionBasis { get; init; }

        /// <summary>
        /// Gets whether the member has its own success evidence (not inherited from an outside confirmation).
        /// </summary>
        public bool IsOwnSuccess { get; init; }

        /// <summary>
        /// Gets whether the member has any financial evidence (completed, paid, confirmed, or an unpaid Payed donation).
        /// </summary>
        public bool HasFinancialEvidence { get; init; }

        /// <summary>
        /// Gets whether the member has a positive paid amount.
        /// </summary>
        public bool IsPaid => this.Input.Paid > 0;
    }
}
