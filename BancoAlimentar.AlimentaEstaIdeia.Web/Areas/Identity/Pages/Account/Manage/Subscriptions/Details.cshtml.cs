// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
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
        private readonly UserManager<WebUser> userManager;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// <param name="context">UnitOfWork.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        /// </summary>
        public DetailsModel(
            IUnitOfWork context,
            UserManager<WebUser> userManager,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.userManager = userManager;
            this.telemetryClient = telemetryClient;
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
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(Guid? publicId)
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

            var user = await userManager.GetUserAsync(User);
            if (!(user != null && userManager != null && user.Id == Subscription.User?.Id))
            {
                this.telemetryClient.TrackEvent(
                    "WhenDeletingSubscripionUserIsNotValidGetDetails",
                    new Dictionary<string, string>()
                    {
                                        { "CurrentLoggedUser", user?.Id },
                                        { "SubcriptionId", Subscription?.Id.ToString() },
                                        { "SubscriptionUser", Subscription.User?.Id },
                    });
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Return the donations associated to the subscription.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>Json.</returns>
        public IActionResult OnGetDataTableData(int id)
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
