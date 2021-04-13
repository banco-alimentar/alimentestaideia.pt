// -----------------------------------------------------------------------
// <copyright file="PaymentItem.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Represent a payment for a donation.
    /// </summary>
    public class PaymentItem
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the donation for this payment.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the payment.
        /// </summary>
        public BasePayment Payment { get; set; }
    }
}
