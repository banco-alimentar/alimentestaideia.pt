// -----------------------------------------------------------------------
// <copyright file="GroupPattern.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Known historical patterns that explain how a duplicate group was created.
    /// </summary>
    public enum GroupPattern
    {
        /// <summary>
        /// The pattern is not recognised.
        /// </summary>
        Unclassified,

        /// <summary>
        /// A later payment row was created and confirmed by the waiting-subscription reconciliation tool
        /// while the original webhook capture rows stayed unlinked.
        /// </summary>
        ReconciliationToolCopy,

        /// <summary>
        /// One confirmed paid payment plus zero-value, uncompleted retries with the same provider ID.
        /// </summary>
        ConfirmedPaymentWithZeroValueRetries,

        /// <summary>
        /// One confirmed paid payment plus unlinked rows that also carry capture evidence.
        /// </summary>
        ConfirmedPaymentWithUnlinkedCaptures,

        /// <summary>
        /// Failed or unknown attempts whose donation was paid by a different payment.
        /// </summary>
        SupersededAttempts,

        /// <summary>
        /// Attempts without any donation link or successful status.
        /// </summary>
        AbandonedOrphanAttempts,

        /// <summary>
        /// Unpaid attempts without a confirmed owner that do not fit a deletion basis.
        /// </summary>
        UnpaidAttempts,

        /// <summary>
        /// One member is confirmed by a donation, but there is no paid evidence for it.
        /// </summary>
        ConfirmedOwnerNotPaid,

        /// <summary>
        /// Simultaneous subscription charges: same timestamp and transaction key, each confirming a different donation.
        /// </summary>
        SimultaneousSubscriptionCharges,

        /// <summary>
        /// Charges on different dates reused one provider ID, each confirming a different donation.
        /// </summary>
        RecurringChargesSharingId,

        /// <summary>
        /// Several confirmed members that do not fit a known subscription pattern.
        /// </summary>
        MultipleConfirmedOwners,

        /// <summary>
        /// A member is linked to a Payed donation that lacks a confirmed payment.
        /// </summary>
        UnconfirmedPaidDonation,

        /// <summary>
        /// A member has capture evidence without a donation confirmation.
        /// </summary>
        UnconfirmedCapture,
    }
}
