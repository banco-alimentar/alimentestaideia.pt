namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class ThanksModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;

        public ThanksModel(
            UserManager<WebUser> userManager)
        {
            this.userManager = userManager;
        }

        public int DonationId { get; set; }

        public WebUser CurrentUser { get; set; }

        public async Task OnGet(int id)
        {
            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            this.DonationId = id;
        }
    }
}
