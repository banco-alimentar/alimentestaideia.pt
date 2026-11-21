// -----------------------------------------------------------------------
// <copyright file="DecisionRule.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Group classification ported from <c>MergeDuplicateEasyPayPaymentIds.sql</c>, evaluated in declaration order.
    /// </summary>
    public enum DecisionRule
    {
        /// <summary>
        /// More than one member is confirmed by a donation.
        /// </summary>
        MultiConfirmed,

        /// <summary>
        /// A member is linked to a Payed donation that has no confirmed payment.
        /// </summary>
        ReviewUnconfirmedPaidDonation,

        /// <summary>
        /// Exactly one member is confirmed once by a Payed donation, is successful, and its paid amount matches.
        /// </summary>
        ConfirmedPaid,

        /// <summary>
        /// Exactly one member is confirmed, but the paid checks fail.
        /// </summary>
        ReviewConfirmedOwner,

        /// <summary>
        /// No member is confirmed, completed, paid, or successful by its own evidence.
        /// </summary>
        NoOwnerRetire,

        /// <summary>
        /// Any other combination; a member may have captured money without a confirmation.
        /// </summary>
        ReviewUnconfirmedCapture,
    }
}
