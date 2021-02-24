namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class ThanksModel : PageModel
    {
        public int DonationId { get; set; }

        public void OnGet(int id)
        {
            this.DonationId = id;
        }
    }
}
