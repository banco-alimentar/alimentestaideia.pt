// -----------------------------------------------------------------------
// <copyright file="DonationDetail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Every stored column and related record for one donation.
    /// </summary>
    public sealed record DonationDetail
    {
        /// <summary>
        /// Gets the donation ID.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Gets how the donation relates to the group.
        /// </summary>
        public IReadOnlyList<string> Relations { get; init; } = new List<string>();

        /// <summary>
        /// Gets the raw columns as stored.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string?>> Columns { get; init; } = new List<KeyValuePair<string, string?>>();

        /// <summary>
        /// Gets the donor.
        /// </summary>
        public DonorInfo? Donor { get; init; }

        /// <summary>
        /// Gets the food bank name.
        /// </summary>
        public string? FoodBankName { get; init; }

        /// <summary>
        /// Gets the donation lines.
        /// </summary>
        public IReadOnlyList<DonationItemInfo> Items { get; init; } = new List<DonationItemInfo>();

        /// <summary>
        /// Gets the invoices.
        /// </summary>
        public IReadOnlyList<InvoiceInfo> Invoices { get; init; } = new List<InvoiceInfo>();

        /// <summary>
        /// Gets the subscriptions that created or own the donation.
        /// </summary>
        public IReadOnlyList<SubscriptionInfo> Subscriptions { get; init; } = new List<SubscriptionInfo>();

        /// <summary>
        /// Gets every payment linked to the donation or confirming it.
        /// </summary>
        public IReadOnlyList<RelatedPaymentInfo> Payments { get; init; } = new List<RelatedPaymentInfo>();
    }
}
