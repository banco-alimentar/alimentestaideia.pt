// -----------------------------------------------------------------------
// <copyright file="NegativePaymentCompletionTimeRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;

    /// <summary>
    /// Donation/payment row with negative completion time delta.
    /// </summary>
    public sealed class NegativePaymentCompletionTimeRow
    {
        /// <summary>
        /// Gets or sets the donation id.
        /// </summary>
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the payment id.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Gets or sets the donation date.
        /// </summary>
        public DateTime DonationDate { get; set; }

        /// <summary>
        /// Gets or sets when the payment was created.
        /// </summary>
        public DateTime PaymentCreated { get; set; }

        /// <summary>
        /// Gets or sets when the payment was completed.
        /// </summary>
        public DateTime? PaymentCompleted { get; set; }

        /// <summary>
        /// Gets or sets the difference in seconds between donation date and completion.
        /// </summary>
        public int? DiffSeconds { get; set; }
    }
}
