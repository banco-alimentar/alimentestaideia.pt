// <copyright file="Donation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Represent a donation from a user.
    /// </summary>
    public class Donation
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the public id for the donation.
        /// Used in the payment system.
        /// </summary>
        [PersonalData]
        public Guid PublicId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WebUser"/> associated with the donation.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the donation datetime.
        /// </summary>
        [PersonalData]
        public DateTime DonationDate { get; set; }

        /// <summary>
        /// Gets or sets the service amount for the donation.
        /// </summary>
        [Column(TypeName = "decimal(10, 2)")]
        [PersonalData]
        public double DonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FoodBank"/> that the user make a donation.
        /// </summary>
        [PersonalData]
        public FoodBank FoodBank { get; set; }

        /// <summary>
        /// Gets or sets if the user wants a receipt.
        /// </summary>
        [PersonalData]
        public bool? WantsReceipt { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        [PersonalData]
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the collection of this belong to this donation.
        /// </summary>
        [PersonalData]
        public virtual ICollection<DonationItem> DonationItems { get; set; }

        /// <summary>
        /// Gets or sets the MultiBank Entity value.
        /// </summary>
        [StringLength(10)]
        [PersonalData]
        public string ServiceEntity { get; set; }

        /// <summary>
        /// Gets or sets the MultiBank Service Reference.
        /// </summary>
        [StringLength(20)]
        [PersonalData]
        public string ServiceReference { get; set; }

        /// <summary>
        /// Gets or sets the payments that this donation has.
        /// </summary>
        [ForeignKey("Donation")]
        public virtual ICollection<BasePayment> PaymentList { get; set; }

        /// <summary>
        /// Gets or sets the Referral Entity.
        /// </summary>
        [PersonalData]
        public Referral ReferralEntity { get; set; }

        /// <summary>
        /// Gets or sets the NIF for the user.
        /// This Nif is only saved when the user is not registered.
        /// </summary>
        [Column("NIF")]
        [StringLength(20)]
        [PersonalData]
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the confirmedpayment for this donation.
        /// </summary>
        [PersonalData]
        public BasePayment ConfirmedPayment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the donation uses cash donation.
        /// </summary>
        public bool IsCashDonation { get; set; }

        /// <summary>
        /// Gets or sets the campaign name.
        /// </summary>
        public string CampaignName { get; set; }

        /// <summary>
        /// Gets or sets the type of custom donation.
        /// </summary>
        public string TypeOfCustomDonation { get; set; }
    }
}