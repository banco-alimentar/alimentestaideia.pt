// -----------------------------------------------------------------------
// <copyright file="InvoiceInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;

    /// <summary>
    /// Invoice issued for a donation.
    /// </summary>
    /// <param name="Id">Invoice ID.</param>
    /// <param name="Number">Invoice number.</param>
    /// <param name="Year">Year.</param>
    /// <param name="Sequence">Sequence.</param>
    /// <param name="Created">Creation time.</param>
    /// <param name="IsCanceled">Whether the invoice is cancelled.</param>
    /// <param name="FoodBankName">Food bank.</param>
    /// <param name="InternalMessage">Internal message.</param>
    public sealed record InvoiceInfo(int Id, string? Number, int Year, int Sequence, DateTime Created, bool IsCanceled, string? FoodBankName, string? InternalMessage);
}
