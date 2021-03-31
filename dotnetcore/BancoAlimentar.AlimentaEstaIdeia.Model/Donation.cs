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
        public Guid PublicId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WebUser"/> associated with the donation.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the donation datetime.
        /// </summary>
        public DateTime DonationDate { get; set; }

        /// <summary>
        /// Gets or sets the service amount for the donation.
        /// </summary>
        [Column(TypeName = "decimal(10, 2)")]
        public double DonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="FoodBank"/> that the user make a donation.
        /// </summary>
        public FoodBank FoodBank { get; set; }

        /// <summary>
        /// Gets or sets if the user wants a receipt.
        /// </summary>
        public bool? WantsReceipt { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Referral for the donation.
        /// </summary>
        public string Referral { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the collection of this belong to this donation.
        /// </summary>
        public virtual ICollection<DonationItem> DonationItems { get; set; }

        /// <summary>
        /// Gets or sets the MultiBank Entity value.
        /// </summary>
        [StringLength(10)]
        public string ServiceEntity { get; set; }

        /// <summary>
        /// Gets or sets the MultiBank Service Reference.
        /// </summary>
        [StringLength(20)]
        public string ServiceReference { get; set; }
    }
}