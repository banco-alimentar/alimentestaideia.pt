// <copyright file="Referral.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Represent a referral code for a campain.
    /// </summary>
    public class Referral
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets code of the referral.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="WebUser"/> associated with the donation.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the list of donations of this referral.
        /// </summary>
        public virtual ICollection<Donation> Donations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets the Status of this referal to Active or not.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the referral can view publicly.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the name of the referral.
        /// </summary>
        public string Name { get; set; }
    }
}