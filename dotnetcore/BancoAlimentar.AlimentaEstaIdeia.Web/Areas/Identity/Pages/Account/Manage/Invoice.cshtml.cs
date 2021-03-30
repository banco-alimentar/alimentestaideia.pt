namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
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

            var all = this.localizer.GetAllStrings();
        }

        public Invoice Invoice { get; set; }

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

                return Page();
            }
            else
            {
                return this.Redirect("./Index");
            }
        }
    }
}
