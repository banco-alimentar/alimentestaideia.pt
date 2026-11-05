// -----------------------------------------------------------------------
// <copyright file="DonationItemInfo.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    /// <summary>
    /// Donation line.
    /// </summary>
    /// <param name="Id">Item ID.</param>
    /// <param name="ProductName">Product name.</param>
    /// <param name="Quantity">Quantity.</param>
    /// <param name="Price">Unit price.</param>
    public sealed record DonationItemInfo(int Id, string? ProductName, int Quantity, double Price);
}
