// <copyright file="MultiBankPayment.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model;

/// <summary>
/// MultiBank payment system.
/// </summary>
public class MultiBankPayment : EasyPayWithValuesBaseClass
{
    /// <summary>
    /// Gets or sets the transcation type.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; }
}
