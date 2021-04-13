// -----------------------------------------------------------------------
// <copyright file="MBWayPayment.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Represent a payment in the MBWay system.
    /// </summary>
    public class MBWayPayment : EasyPayWithValuesBaseClass
    {
        /// <summary>
        /// Gets or sets the alias.
        /// </summary>
        public string Alias { get; set; }
    }
}
