// -----------------------------------------------------------------------
// <copyright file="CancelSubscription.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Cancel subscription model.
    /// </summary>
    public class CancelSubscriptionModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelSubscriptionModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="context">Unit of work.</param>
        public CancelSubscriptionModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        [BindProperty]
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
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
