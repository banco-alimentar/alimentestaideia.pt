// -----------------------------------------------------------------------
// <copyright file="InvoicingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

/// <summary>
/// Define the Invoice strategy.
/// </summary>
public enum InvoicingStrategy : int
{
    /// <summary>
    /// Single invoice for all Food Banks.
    /// </summary>
    SingleInvoiceTable = 0,

    /// <summary>
    /// Multiple invoice tables per food bank.
    /// </summary>
    MultipleTablesPerFoodBank = 1,
}
