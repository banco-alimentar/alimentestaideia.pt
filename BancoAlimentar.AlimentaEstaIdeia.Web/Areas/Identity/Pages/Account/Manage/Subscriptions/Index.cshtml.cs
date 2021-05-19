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
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

        public async Task<IActionResult> OnGetDataTableData()
        {
            var user = await userManager.GetUserAsync(User);
            var subscriptions = context.SubscriptionRepository.GetUserSubscription(user);

            JArray list = new JArray();
            int count = 1;
            foreach (var item in subscriptions)
            {
                JObject obj = new JObject();
                obj.Add("Id", count);
                obj.Add("Created", item.Created);
                obj.Add("ExpirationTime", item.ExpirationTime);
                obj.Add("StartTime", item.StartTime);
                obj.Add("SubscriptionType", item.SubscriptionType.ToString());

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
