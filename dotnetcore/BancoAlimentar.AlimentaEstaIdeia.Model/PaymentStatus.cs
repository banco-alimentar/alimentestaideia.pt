// <copyright file="PaymentStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Payment status enum.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// The donation is payed.
        /// </summary>
        Payed = 1,

        /// <summary>
        /// The donation is not yet payed.
        /// </summary>
        NotPayed = 2,

        /// <summary>
        /// Waiting for the donation to be payed.
        /// </summary>
        WaitingPayment = 3,
    }
}
