namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.FeatureManagement.Mvc;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Display the detail for a subscription.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.SubscriptionAdmin)]
    public class DetailsModel : PageModel
    {
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">UnitOfWork.</param>
        public DetailsModel(IUnitOfWork context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the actual Subscription.
        /// </summary>
        [BindProperty]
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Gets or sets the list of donations part of the Subscription.
        /// </summary>
        [BindProperty]
        public List<Donation> Donations { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Subscription public id.</param>
        /// <returns>Task.</returns>
        public IActionResult OnGet(Guid? publicId)
        {
            if (publicId == null)
            {
                return NotFound();
            }

            Subscription = context.SubscriptionRepository.GetSubscriptionByPublicId(publicId.Value);

            if (Subscription == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnGetDataTableData(int id)
        {
            var donations = context.SubscriptionRepository.GetDonationsForSubscription(id);

            JArray list = new JArray();
            int count = 1;
            foreach (var item in donations)
            {
                JObject obj = new JObject();
                obj.Add("Id", count);
                obj.Add("Created", item.DonationDate.ToString("g"));
                obj.Add("Amount", item.DonationAmount);
                obj.Add("FoodBank", item.FoodBank != null ? item.FoodBank.Name : string.Empty);
                obj.Add("Payment", item.PaymentStatus.ToString());
                obj.Add("PublicId", item.PublicId);
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
