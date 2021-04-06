namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using DNTCaptcha.Core;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Localization;
    using Humanizer;

    public class InvoiceModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IStringLocalizer localizer;

        public InvoiceModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.userManager = userManager;
            this.context = context;
            this.localizer = stringLocalizerFactory.Create("Areas.Identity.Pages.Account.Manage.Invoice", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
        }

        public Invoice Invoice { get; set; }

        public string DonationAmountToText { get; set; }

        public void ConvertAmountToText()
        {
            // Still need to take care of localization
            ConvertCurrencyToText(Invoice.Donation.DonationAmount, "pt-pt", "Euro", "Euros", "cêntimo", "cêntimos", "e");
        }

        /// <summary>
        /// Converts a currency double to it's written representation (assumes a double with 2 fractional digits)
        /// </summary>
        /// <param name="amount">The currency amount to convert to text description</param>
        /// <param name="culture">the culture that will be used to convert</param>
        /// <param name="currencyOne">Currency name in singular, eg Euro, Dolar</param>
        /// <param name="currencyMany">Currency name in Plural, eg Euros, Dolars</param>
        /// <param name="centOne">The name of one cent in the given culture</param>
        /// <param name="centMany">The name of cents in the given culture</param>
        /// <param name="andstring">The "and" separator in that language for X euros "and" Y cents</param>
        /// <returns>The written description of the currency in that language</returns>
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
                    if (integerPart == 0)
                    {
                        return fractionalPart.ToWords(cultureInfo) + OneOrManyCurrency(fractionalPart, centOne, centMany);
                    }
                    else
                    {
                        return integerPart.ToWords(cultureInfo) + OneOrManyCurrency(integerPart, currencyOne, currencyMany) + " " + andstring + " " + fractionalPart.ToWords(cultureInfo) + OneOrManyCurrency(fractionalPart, centOne, centMany); ;
                    }

                // More than two segments means it's invalid, so throw an exception
                default:
                    throw new Exception("Something bad happened in ConvertAmountToText!");
            }
        }
        /// <summary>
        /// Returns the unit or multiple description according to the provided value if 1 or many
        /// </summary>
        /// <param name="value">the value to describe</param>
        /// <param name="one">the unit representation</param>
        /// <param name="many">the many representation</param>
        /// <returns>the one or many string</returns>
        private static string OneOrManyCurrency(long value, string one, string many)
        {
            if (value == 1)
            { return " " + one; }
            else
            { return " " + many; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await userManager.GetUserAsync(User);

            Invoice = this.context.Invoice.FindInvoiceByDonation(id, user);

            if (Invoice != null && Invoice.User != null)
            {
                if (Invoice.User.Address == null)
                {
                    Invoice.User.Address = new DonorAddress()
                    {
                        Address1 = string.Empty,
                        Address2 = string.Empty,
                        City = string.Empty,
                        Country = string.Empty,
                        PostalCode = string.Empty,
                    };
                }

                ConvertAmountToText();

                return Page();
            }
            else
            {
                return this.Redirect("./Index");
            }
        }
    }
}
