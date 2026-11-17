// -----------------------------------------------------------------------
// <copyright file="PaymentMemberInput.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Analysis
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Raw database evidence for one payment in a duplicate group.
    /// </summary>
    public sealed record PaymentMemberInput
    {
        /// <summary>
        /// Gets the payment identifier.
        /// </summary>
        public int PaymentId { get; init; }

        /// <summary>
        /// Gets the stored provider identifier, exactly as stored on this row.
        /// </summary>
        public string EasyPayPaymentId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the payment discriminator.
        /// </summary>
        public string PaymentType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the creation timestamp.
        /// </summary>
        public DateTime Created { get; init; }

        /// <summary>
        /// Gets the completion timestamp.
        /// </summary>
        public DateTime? Completed { get; init; }

        /// <summary>
        /// Gets the provider status.
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Gets the provider transaction key.
        /// </summary>
        public string? TransactionKey { get; init; }

        /// <summary>
        /// Gets the requested amount.
        /// </summary>
        public float? Requested { get; init; }

        /// <summary>
        /// Gets the paid amount.
        /// </summary>
        public float? Paid { get; init; }

        /// <summary>
        /// Gets the forward donation link (<c>Payments.DonationId</c>).
        /// </summary>
        public int? DonationId { get; init; }

        /// <summary>
        /// Gets the donation referenced by <see cref="DonationId"/>.
        /// </summary>
        public DonationFacts? ForwardDonation { get; init; }

        /// <summary>
        /// Gets the donations whose <c>ConfirmedPaymentId</c> is this payment.
        /// </summary>
        public IReadOnlyList<DonationFacts> ConfirmingDonations { get; init; } = Array.Empty<DonationFacts>();

        /// <summary>
        /// Gets the payment that confirms the forward donation, when there is one.
        /// </summary>
        public ReplacementPaymentFacts? Replacement { get; init; }
    }
}
