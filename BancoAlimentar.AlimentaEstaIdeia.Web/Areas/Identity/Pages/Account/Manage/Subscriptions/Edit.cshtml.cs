// -----------------------------------------------------------------------
// <copyright file="Edit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.FeatureManagement.Mvc;

    /// <summary>
    /// Edits the subscription.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.SubscriptionAdmin)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<WebUser> userManager;
        private readonly EasyPayBuilder easyPayBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditModel"/> class.
        /// </summary>
        /// <param name="context">EF Db Context.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="easyPayBuilder">EasyPay API builder.</param>
        public EditModel(
            ApplicationDbContext context,
            UserManager<WebUser> userManager,
            EasyPayBuilder easyPayBuilder)
        {
            this.context = context;
            this.userManager = userManager;
            this.easyPayBuilder = easyPayBuilder;
        }

        /// <summary>
        /// Gets or sets the current subscription.
        /// </summary>
        [BindProperty]
        public AlimentaEstaIdeia.Model.Subscription Subscription { get; set; }

        /// <summary>
        /// Executes the get operation.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>Task.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Subscription = await context.Subscriptions.FirstOrDefaultAsync(m => m.Id == id);

            if (Subscription == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            if (!(user != null && userManager != null && user.Id == Subscription.User?.Id))
            {
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Executed the post operation.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            DateTime newExpirationTime = Subscription.ExpirationTime;

            if (newExpirationTime < DateTime.UtcNow)
            {
                ModelState.AddModelError("ExpirationDate", "Expiration date should be in the future.");
            }

            if (ModelState.IsValid)
            {
                Subscription = await this.context.Subscriptions.FirstOrDefaultAsync(m => m.Id == Subscription.Id);
                Subscription.ExpirationTime = newExpirationTime;

                var user = await userManager.GetUserAsync(User);
                if (!(user != null && userManager != null && user.Id == Subscription.User?.Id))
                {
                    return NotFound();
                }

                try
                {
                    this.easyPayBuilder.GetSubscriptionPaymentApi().SubscriptionIdPatch(
                        Guid.Parse(Subscription.EasyPaySubscriptionId), new SubscriptionIdPatchRequest()
                        {
                            ExpirationTime = Subscription.ExpirationTime.GetEasyPayDateTimeString(),
                        });
                    await context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    if (!SubscriptionExists(Subscription.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToPage("./Index");
            }
            else
            {
                if (int.TryParse(Request.Form["Subscription.Id"].First(), out int subscriptionId))
                {
                    Subscription = await context.Subscriptions.FirstOrDefaultAsync(m => m.Id == subscriptionId);
                    return Page();
                }
                else
                {
                    return RedirectToPage("./Index");
                }
            }
        }

        private bool SubscriptionExists(int id)
        {
            return context.Subscriptions.Any(e => e.Id == id);
        }
    }
}
