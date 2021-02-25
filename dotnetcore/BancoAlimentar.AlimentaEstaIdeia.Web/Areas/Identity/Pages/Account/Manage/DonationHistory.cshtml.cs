namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class DonationHistoryModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        public DonationHistoryModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        public List<Donation> Donations { get; set; }

        public async Task OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            this.Donations = this.context.Donation.GetUserDonation(user.Id);

            
        }
    }
}
