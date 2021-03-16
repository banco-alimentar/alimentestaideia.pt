namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;

    public class ThanksModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IStringLocalizer localizer;

        public ThanksModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IViewLocalizer localizer,
            IHtmlLocalizer<ThanksModel> html,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.userManager = userManager;
            this.context = context;
            this.localizer = stringLocalizerFactory.Create("Pages.Thanks", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
        }

        public int DonationId { get; set; }

        public WebUser CurrentUser { get; set; }

        [BindProperty]
        public string TwittMessage { get; set; }

        public async Task OnGet(int id)
        {
#if RELEASE
            id = 0;
#endif
            if (TempData["Donation"] != null)
            {
                id = (int)TempData["Donation"];
            }

            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            this.DonationId = id;

            Donation donation = this.context.Donation.GetFullDonationById(id);
            if (donation != null)
            {
                TwittMessage = string.Format(localizer.GetString("TwittMessage"), donation.ServiceAmount, donation.FoodBank.Name);
            }
        }
    }
}
