// -----------------------------------------------------------------------
// <copyright file="StatusSource.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    /// <summary>
    /// Where a member's success evidence comes from when the payment status is missing.
    /// </summary>
    public enum StatusSource
    {
        /// <summary>
        /// The payment row has its own status.
        /// </summary>
        Payment,

        /// <summary>
        /// The status falls back to the donation referenced by <c>Payments.DonationId</c>.
        /// </summary>
        Donation,

        /// <summary>
        /// The status falls back to the single donation that confirms the payment.
        /// </summary>
        ConfirmedDonation,

        /// <summary>
        /// No status evidence is available.
        /// </summary>
        Unknown,
    }
}
