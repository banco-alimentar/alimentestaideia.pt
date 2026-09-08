// -----------------------------------------------------------------------
// <copyright file="EasyPaySubscriptionPaymentEvidence.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay
{
    using System;

    /// <summary>
    /// Verified payment data returned as part of an Easypay subscription.
    /// </summary>
    public sealed class EasyPaySubscriptionPaymentEvidence
    {
        /// <summary>
        /// Gets or sets the Easypay subscription identifier.
        /// </summary>
        public string EasypaySubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the individual Easypay transaction identifier.
        /// </summary>
        public string EasypayPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the merchant transaction key.
        /// </summary>
        public string TransactionKey { get; set; }

        /// <summary>
        /// Gets or sets the provider transaction date.
        /// </summary>
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Gets or sets the amount requested by Easypay.
        /// </summary>
        public decimal Requested { get; set; }

        /// <summary>
        /// Gets or sets the amount paid according to Easypay.
        /// </summary>
        public decimal Paid { get; set; }

        /// <summary>
        /// Gets or sets the fixed fee reported by Easypay.
        /// </summary>
        public decimal FixedFee { get; set; }

        /// <summary>
        /// Gets or sets the variable fee reported by Easypay.
        /// </summary>
        public decimal VariableFee { get; set; }

        /// <summary>
        /// Gets or sets the tax reported by Easypay.
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Gets or sets the transferred amount reported by Easypay.
        /// </summary>
        public decimal Transfer { get; set; }
    }
}
