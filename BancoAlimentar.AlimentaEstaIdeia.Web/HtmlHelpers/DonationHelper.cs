// -----------------------------------------------------------------------
// <copyright file="DonationHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.HtmlHelpers
{
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    public class DonationHelper
    {
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
}
