// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.FeatureManagement.Mvc;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [FeatureGate(DevelopingFeatureFlags.SubscriptionAdmin)]
    public class IndexModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        public IndexModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetDataTableDataAsync()
        {
            var user = await userManager.GetUserAsync(User);
            var subscriptions = context.SubscriptionRepository.GetUserSubscription(user);

            JArray list = new JArray();
            int count = 1;
            foreach (var item in subscriptions)
            {
                JObject obj = new JObject();
                obj.Add("Id", count);
                obj.Add("Created", item.Created.ToString("g"));
                obj.Add("ExpirationTime", item.ExpirationTime.ToString("g"));
                obj.Add("StartTime", item.StartTime.ToString("g"));
                obj.Add("SubscriptionType", item.SubscriptionType.ToString());
                obj.Add("Status", item.Status.ToString());
                obj.Add("Frecuency", item.Frequency);
                obj.Add("DonationAmount", item.InitialDonation.DonationAmount);
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
