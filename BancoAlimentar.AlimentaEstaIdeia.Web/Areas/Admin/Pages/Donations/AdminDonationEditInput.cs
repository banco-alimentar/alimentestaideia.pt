// -----------------------------------------------------------------------
// <copyright file="AdminDonationEditInput.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Allow-listed fields for admin donation edits (excludes payment and identity fields).
    /// </summary>
    public class AdminDonationEditInput
    {
        /// <summary>
        /// Gets or sets the donation identifier.
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the donation datetime.
        /// </summary>
        [Required]
        [Display(Name = "Donation date")]
        public DateTime DonationDate { get; set; }

        /// <summary>
        /// Gets or sets if the donor wants a receipt.
        /// </summary>
        [Display(Name = "Wants receipt")]
        public bool? WantsReceipt { get; set; }

        /// <summary>
        /// Gets or sets the multibanco entity value.
        /// </summary>
        [StringLength(10)]
        [Display(Name = "Service entity")]
        public string ServiceEntity { get; set; }

        /// <summary>
        /// Gets or sets the multibanco service reference.
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Service reference")]
        public string ServiceReference { get; set; }

        /// <summary>
        /// Maps an existing donation to editable input fields.
        /// </summary>
        /// <param name="donation">Donation entity.</param>
        /// <returns>Editable input model.</returns>
        public static AdminDonationEditInput FromDonation(Donation donation)
        {
            return new AdminDonationEditInput
            {
                Id = donation.Id,
                DonationDate = donation.DonationDate,
                WantsReceipt = donation.WantsReceipt,
                ServiceEntity = donation.ServiceEntity,
                ServiceReference = donation.ServiceReference,
            };
        }

        /// <summary>
        /// Applies allow-listed values to a tracked donation entity.
        /// </summary>
        /// <param name="donation">Donation entity to update.</param>
        public void ApplyTo(Donation donation)
        {
            donation.DonationDate = this.DonationDate;
            donation.WantsReceipt = this.WantsReceipt;
            donation.ServiceEntity = this.ServiceEntity;
            donation.ServiceReference = this.ServiceReference;
        }
    }
}
