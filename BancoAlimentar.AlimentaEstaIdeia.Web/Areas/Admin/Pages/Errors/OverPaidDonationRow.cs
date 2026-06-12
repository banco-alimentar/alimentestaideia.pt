// -----------------------------------------------------------------------
// <copyright file="OverPaidDonationRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;

    /// <summary>
    /// Donation where total paid across payments exceeds the donation amount.
    /// </summary>
    public sealed class OverPaidDonationRow
    {
        /// <summary>
        /// Gets or sets the donation id.
        /// </summary>
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the donation date.
        /// </summary>
        public DateTime DonationDate { get; set; }

        /// <summary>
        /// Gets or sets the donation amount.
        /// </summary>
        public decimal DonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the sum of paid values across all payments.
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        /// Gets or sets the difference between total paid and donation amount.
        /// </summary>
        public decimal OverPaid { get; set; }

        /// <summary>
        /// Gets or sets the number of payments linked to the donation.
        /// </summary>
        public int PaymentCount { get; set; }
    }
}
