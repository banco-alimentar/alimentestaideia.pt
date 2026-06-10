// -----------------------------------------------------------------------
// <copyright file="BigDonationRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Large donation row.
    /// </summary>
    public sealed class BigDonationRow
    {
        /// <summary>
        /// Gets or sets the donation id.
        /// </summary>
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the donor full name.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the donor email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the donation date.
        /// </summary>
        public DateTime DonationDate { get; set; }

        /// <summary>
        /// Gets or sets the donation amount.
        /// </summary>
        public double DonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the food bank name.
        /// </summary>
        public string FoodBankName { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the confirmed payment id.
        /// </summary>
        public int? ConfirmedPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the donor NIF.
        /// </summary>
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the donor wants a receipt.
        /// </summary>
        public bool? WantsReceipt { get; set; }
    }
}
