// -----------------------------------------------------------------------
// <copyright file="Subscription.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Represent a subscription.
    /// </summary>
    public class Subscription
    {
        /// <summary>
        /// Gets or sets the table Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the PublicId for the subscription.
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// Gets or sets when the Subscription was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets when the Subscription will start.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the expiration time for the subscription.
        /// </summary>
        public DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets the easypay subscription id.
        /// </summary>
        public string EasyPaySubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the donation items in this format.
        /// 1:1;2:2;3:3;4:4;5:5;6:6;.
        /// First element is Product CatalogId, next one is quantity.
        /// </summary>
        public string DonationItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subscription is market as deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the easypay transaction id.
        /// </summary>
        public string EasyPayTransactionId { get; set; }

        /// <summary>
        /// Gets or sets the transaction key.
        /// </summary>
        public string TransactionKey { get; set; }

        /// <summary>
        /// Gets or sets the frequency for the subscription.
        /// </summary>
        public string Frequency { get; set; }

        /// <summary>
        /// Gets or sets the easypay payment url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the initial donation that generate this subscription.
        /// </summary>
        public Donation InitialDonation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the status of the subscription.
        /// </summary>
        public SubscriptionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the payment type. Credit Card (CC) or Direct Deposit(DD).
        /// </summary>
        public SubscriptionPaymentType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Donation"/> related to this <see cref="Subscription"/>.
        /// </summary>
        public ICollection<SubscriptionDonations> Donations { get; set; }
    }
}
