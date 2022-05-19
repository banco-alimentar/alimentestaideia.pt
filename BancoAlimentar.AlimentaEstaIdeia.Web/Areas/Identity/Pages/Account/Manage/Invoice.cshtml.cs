// -----------------------------------------------------------------------
// <copyright file="Invoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;

using System;
using System.Globalization;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Web.Model;
using Humanizer;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Invoice render model.
/// </summary>
public class InvoiceModel : PageModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceModel"/> class.
    /// </summary>
    public InvoiceModel()
    {
    }

    /// <summary>
    /// Gets or sets the invoice render model.
    /// </summary>
    public InvoiceRenderModel InvoiceRenderModel { get; set; }

    /// <summary>
    /// Gets or sets the full name for the user.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets the Nif for the user.
    /// </summary>
    public string Nif { get; set; }

    /// <summary>
    /// Gets or sets the donation amount.
    /// </summary>
    public double DonationAmount { get; set; }

    /// <summary>
    /// Gets or sets the current <see cref="Campaign"/>.
    /// </summary>
    public Campaign Campaign { get; set; }

    /// <summary>
    /// Gets or sets the Invoice name.
    /// </summary>
    public string InvoiceName { get; set; }

    /// <summary>
    /// Gets or sets the donation amount in text.
    /// </summary>
    public string DonationAmountToText { get; set; }

    /// <summary>
    /// Converts a currency double to it's written representation (assumes a double with 2 fractional digits).
    /// </summary>
    /// <param name="amount">The currency amount to convert to text description.</param>
    /// <param name="culture">the culture that will be used to convert.</param>
    /// <param name="currencyOne">Currency name in singular, eg Euro, Dolar.</param>
    /// <param name="currencyMany">Currency name in Plural, eg Euros, Dolars.</param>
    /// <param name="centOne">The name of one cent in the given culture.</param>
    /// <param name="centMany">The name of cents in the given culture.</param>
    /// <param name="andstring">The "and" separator in that language for X euros "and" Y cents.</param>
    /// <returns>The written description of the currency in that language.</returns>
    public static string ConvertCurrencyToText(double amount, string culture, string currencyOne, string currencyMany, string centOne, string centMany, string andstring)
    {
        CultureInfo cultureInfo = CultureInfo.GetCultureInfo(culture);

        long integerPart = (long)Math.Truncate(amount);
        string amountStr = amount.ToString("#.##", cultureInfo);

        // Get the decimal separator the specified culture
        char[] sep = cultureInfo.NumberFormat.NumberDecimalSeparator.ToCharArray();

        // Split the string on the separator
        string[] segments = amountStr.Split(sep);

        switch (segments.Length)
        {
            // Only one segment, so there was not fractional value
            case 1:
                return integerPart.ToWords(cultureInfo) + OneOrManyCurrency(integerPart, currencyOne, currencyMany);

            case 2:
                int fractionalPart = Convert.ToInt32(segments[1]);
                if (segments[1].Length == 1)
                {
                    fractionalPart *= 10;
                }

                if (integerPart == 0)
                {
                    return fractionalPart.ToWords(cultureInfo) + OneOrManyCurrency(fractionalPart, centOne, centMany);
                }
                else
                {
                    return string.Concat(
                        integerPart.ToWords(cultureInfo),
                        OneOrManyCurrency(integerPart, currencyOne, currencyMany),
                        " ",
                        andstring,
                        " ",
                        fractionalPart.ToWords(cultureInfo),
                        OneOrManyCurrency(fractionalPart, centOne, centMany));
                }

            // More than two segments means it's invalid, so throw an exception
            default:
                {
                    throw new Exception("Something bad happened in ConvertAmountToText!");
                }
        }
    }

    /// <summary>
    /// Converts Donation.DonationAmount to it's written representation and stores it in DonationAmountToText (assumes a double with 2 fractional digits).
    /// </summary>
    public void ConvertAmountToText()
    {
        // Still need to take care of localization
        DonationAmountToText = ConvertCurrencyToText(DonationAmount, "pt-pt", "euro", "euros", "cêntimo", "cêntimos", "e");
    }

    /// <summary>
    /// Returns the unit or multiple description according to the provided value if 1 or many.
    /// </summary>
    /// <param name="value">the value to describe.</param>
    /// <param name="one">the unit representation.</param>
    /// <param name="many">the many representation.</param>
    /// <returns>the one or many string.</returns>
    private static string OneOrManyCurrency(long value, string one, string many)
    {
        if (value == 1)
        {
            return string.Concat(" ", one);
        }
        else
        {
            return string.Concat(" ", many);
        }
    }
}
