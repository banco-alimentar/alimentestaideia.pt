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
            var textToHuman = new HumanReadableIntegerProvider();
            Language targetLanguage = Language.Portuguese;
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentUICulture;
            if (cultureInfo.TwoLetterISOLanguageName == "es")
            {
                targetLanguage = Language.Spanish;
            }
            else if (cultureInfo.TwoLetterISOLanguageName == "en")
            {
                targetLanguage = Language.English;
            }
            else if (cultureInfo.TwoLetterISOLanguageName == "fr")
            {
                targetLanguage = Language.English;
            }

            string integerPart = textToHuman.GetText((long)Math.Truncate(Invoice.Donation.DonationAmount), targetLanguage);
            string decimalPart = textToHuman.GetText((long)(Math.Round((Invoice.Donation.DonationAmount - Math.Truncate(Invoice.Donation.DonationAmount)) * 100)), targetLanguage);

            DonationAmountToText = string.Concat(integerPart, ", ", decimalPart);
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
