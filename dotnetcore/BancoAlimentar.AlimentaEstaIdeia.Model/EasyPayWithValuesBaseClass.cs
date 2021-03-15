// -----------------------------------------------------------------------
// <copyright file="EasyPayWithValuesBaseClass.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Base class for all easypay payments that support monetary values.
    /// </summary>
    public abstract class EasyPayWithValuesBaseClass : EasyPayBaseClass
    {
        /// <summary>
        /// Gets or sets the transaction requestes payment monetary value.
        /// </summary>
        public float Requested { get; set; }

        /// <summary>
        /// Gets or sets the transaction payed payment monetary value.
        /// </summary>
        public float Paid { get; set; }

        /// <summary>
        /// Gets or sets the transaction fixed feee value.
        /// </summary>
        public float FixedFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction variable fee value.
        /// </summary>
        public float VariableFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction tax value.
        /// </summary>
        public float Tax { get; set; }

        /// <summary>
        /// Gets or sets the transaction transfered monetary value.
        /// </summary>
        public float Transfer { get; set; }
    }
}
