namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class MultibancoModel : PageModel
    {
        private readonly IUnitOfWork context;

        public MultibancoModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public Donation Donation { get; set; }

        public void OnGet(int id)
        {
            Donation = this.context.Donation.GetFullDonationById(id);
        }
    }
}
