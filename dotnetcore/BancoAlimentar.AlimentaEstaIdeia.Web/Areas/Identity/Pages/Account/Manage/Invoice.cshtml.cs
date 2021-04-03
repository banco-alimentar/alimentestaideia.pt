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
            string integerPart = ((long)Math.Truncate(Invoice.Donation.DonationAmount)).ToWords();
            long centimos = (long)Math.Round(Invoice.Donation.DonationAmount);
            if (centimos != 0)
            {
                DonationAmountToText = string.Concat(integerPart, " Euros e, ", centimos.ToWords(), " Cêntimos");
            }
            else
            { DonationAmountToText = string.Concat(integerPart, " Euros"); }
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
