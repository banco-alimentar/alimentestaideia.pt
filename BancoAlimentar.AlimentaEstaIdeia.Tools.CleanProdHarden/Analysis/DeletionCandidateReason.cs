// -----------------------------------------------------------------------
// <copyright file="DeletionCandidateReason.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Why the SQL planner marks a member as a deletion candidate.
    /// </summary>
    public enum DeletionCandidateReason
    {
        /// <summary>
        /// <c>Payments.DonationId</c> is null.
        /// </summary>
        MissingDonationId,

        /// <summary>
        /// The linked Payed donation is confirmed by a payment outside the group.
        /// </summary>
        PaidDonationConfirmedOutsideGroup,

        /// <summary>
        /// The member has a known unsuccessful status.
        /// </summary>
        UnsuccessfulStatus,
    }
}
