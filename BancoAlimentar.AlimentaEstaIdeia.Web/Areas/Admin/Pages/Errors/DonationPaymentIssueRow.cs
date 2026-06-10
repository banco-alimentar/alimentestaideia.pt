// -----------------------------------------------------------------------
// <copyright file="DonationPaymentIssueRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Donation and confirmed payment details for integrity checks.
    /// </summary>
    public sealed class DonationPaymentIssueRow
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
        public double DonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        public PaymentStatus PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the payment discriminator.
        /// </summary>
        public string Discriminator { get; set; }

        /// <summary>
        /// Gets or sets the paid amount on the payment.
        /// </summary>
        public float? Paid { get; set; }

        /// <summary>
        /// Gets or sets when the payment was completed.
        /// </summary>
        public DateTime? Completed { get; set; }

        /// <summary>
        /// Gets or sets the fixed fee.
        /// </summary>
        public float? FixedFee { get; set; }

        /// <summary>
        /// Gets or sets the variable fee.
        /// </summary>
        public float? VariableFee { get; set; }

        /// <summary>
        /// Gets or sets the tax.
        /// </summary>
        public float? Tax { get; set; }

        /// <summary>
        /// Gets or sets the transfer amount.
        /// </summary>
        public float? Transfer { get; set; }

        /// <summary>
        /// Gets or sets the service reference.
        /// </summary>
        public string ServiceReference { get; set; }

        /// <summary>
        /// Gets or sets the service entity.
        /// </summary>
        public string ServiceEntity { get; set; }

        /// <summary>
        /// Gets or sets the payment type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the payment message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the PayPal payment id.
        /// </summary>
        public string PayPalPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the Easypay payment id.
        /// </summary>
        public string EasyPayPaymentId { get; set; }
    }
}
