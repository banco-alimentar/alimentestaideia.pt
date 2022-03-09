// <copyright file="PayPalPayment.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// PayPal payment system.
    /// </summary>
    public class PayPalPayment : BasePayment
    {
        /// <summary>
        /// Gets or sets the PayPal Payment Id.
        /// </summary>
        public string PayPalPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the Payer Id.
        /// </summary>
        public string PayerId { get; set; }

        /// <summary>
        /// Gets or sets the PayPal token.
        /// </summary>
        public string Token { get; set; }
    }
}
