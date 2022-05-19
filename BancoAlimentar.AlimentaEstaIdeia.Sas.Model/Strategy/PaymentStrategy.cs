// -----------------------------------------------------------------------
// <copyright file="PaymentStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

/// <summary>
/// Payment strategy for the tenant.
/// </summary>
public enum PaymentStrategy : int
{
    /// <summary>
    /// Payments are shared for each food bank.
    /// </summary>
    SharedPaymentProcessor = 0,

    /// <summary>
    /// Each food bank has it own configuration for the payment system.
    /// </summary>
    IndividualPaymentProcessorPerFoodBank = 1,
}
