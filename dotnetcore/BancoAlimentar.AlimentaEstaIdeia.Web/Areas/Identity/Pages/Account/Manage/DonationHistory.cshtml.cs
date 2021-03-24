namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

        public async Task<IActionResult> OnGetDataTableData()
        {
            var user = await userManager.GetUserAsync(User);
            var donations = this.context.Donation.GetUserDonation(user.Id);
            JArray list = new JArray();
            int count = 1;
            foreach (var item in donations)
            {
                JObject obj = new JObject();
                obj.Add("Id", count);
                obj.Add("DonationDate", item.DonationDate.ToString());
                obj.Add("FoodBank", item.FoodBank != null ? item.FoodBank.Name : string.Empty);
                obj.Add("ServiceAmount", item.ServiceAmount);
                obj.Add("Invoice", item.Id);
                list.Add(obj);
                count++;
            }

            return new ContentResult()
            {
                Content = JsonConvert.SerializeObject(list),
                ContentType = "application/json",
                StatusCode = 200,
            };
        }
    }
}
