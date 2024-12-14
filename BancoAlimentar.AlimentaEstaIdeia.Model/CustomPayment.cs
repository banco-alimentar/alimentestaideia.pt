// -----------------------------------------------------------------------
// <copyright file="CustomPayment.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Custom payment class.
    /// </summary>
    public class CustomPayment : BasePayment
    {
        /// <summary>
        /// Gets or sets the amount of the payment.
        /// </summary>
        [Column("CustomPaymentAmount")]
        public float Amount { get; set; }

        /// <summary>
        /// Gets or sets the payment type.
        /// </summary>
        public string PaymentType { get; set; }
    }
}
