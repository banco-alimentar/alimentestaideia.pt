// <copyright file="CreditCardPayment.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

/// <summary>
/// Represent a credit card payment.
/// </summary>
public class CreditCardPayment : EasyPayWithValuesBaseClass
{
    /// <summary>
    /// Gets or sets the url that the user need to be redirected to complete the payment.
    /// </summary>
    public string Url { get; set; }
}
