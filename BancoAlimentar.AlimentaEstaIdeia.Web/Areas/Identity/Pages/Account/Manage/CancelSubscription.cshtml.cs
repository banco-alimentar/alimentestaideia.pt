// -----------------------------------------------------------------------
// <copyright file="CancelSubscription.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

    public class CancelSubscriptionModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelSubscriptionModel"/> class.
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="context"></param>
        public CancelSubscriptionModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        [BindProperty]
        public Subscription Subscription { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync(int subscriptionId)
        {
            var user = await userManager.GetUserAsync(User);
            Subscription = context.SubscriptionRepository.GetById(subscriptionId);
            if (Subscription != null)
            {
            }
        }
    }
}
