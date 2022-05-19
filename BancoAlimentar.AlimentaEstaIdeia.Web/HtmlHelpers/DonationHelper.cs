// -----------------------------------------------------------------------
// <copyright file="DonationHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.HtmlHelpers;

using System.Linq;
using BancoAlimentar.AlimentaEstaIdeia.Model;

/// <summary>
/// Donation helper.
/// </summary>
public class DonationHelper
{
    /// <summary>
    /// Gets the donation by index.
    /// </summary>
    /// <param name="index">Index.</param>
    /// <param name="value">Donation value.</param>
    /// <returns>A reference to the <see cref="DonationItem"/>.</returns>
    public static DonationItem GetDonationItemByIndex(int index, Donation value)
    {
        DonationItem result = null;
        result = value.DonationItems.ElementAtOrDefault(index);
        if (result == null)
        {
            result = new DonationItem()
            {
                Quantity = 0,
            };
        }

        return result;
    }
}
