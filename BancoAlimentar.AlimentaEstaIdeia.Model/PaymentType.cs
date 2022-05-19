// -----------------------------------------------------------------------
// <copyright file="PaymentType.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

/// <summary>
/// Define the payment type.
/// </summary>
public enum PaymentType
{
    /// <summary>
    /// No payment.
    /// </summary>
    None,

    /// <summary>
    /// Paypal payment.
    /// </summary>
    Paypal,

    /// <summary>
    /// EasyPay Multibanco.
    /// </summary>
    MultiBanco,

    /// <summary>
    /// EasyPay CreditCard.
    /// </summary>
    CreditCard,

    /// <summary>
    /// Easypay MBWay.
    /// </summary>
    MBWay,
}
