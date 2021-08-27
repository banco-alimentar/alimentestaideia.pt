// -----------------------------------------------------------------------
// <copyright file="Delete.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.FeatureManagement.Mvc;

    /// <summary>
    /// Delete the subscription.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.SubscriptionAdmin)]
    public class DeleteModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work context.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public DeleteModel(
            IUnitOfWork context,
            EasyPayBuilder easyPayBuilder,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.easyPayBuilder = easyPayBuilder;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Gets or sets the active <see cref="Subscription"/>.
        /// </summary>
        [BindProperty]
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>Page.</returns>
        public IActionResult OnGet(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Subscription = this.context.SubscriptionRepository.GetById(id.Value);

            if (Subscription == null)
            {
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>Page.</returns>
        public IActionResult OnPost(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Subscription = this.context.SubscriptionRepository.GetById(id.Value);

            try
            {
                var response = this.easyPayBuilder
                    .GetSubscriptionPaymentApi()
                    .SubscriptionIdDeleteWithHttpInfo(Subscription.EasyPaySubscriptionId);

                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    bool succeed = this.context.SubscriptionRepository.DeleteSubscription(id.Value);

                    if (succeed)
                    {
                        return RedirectToPage("./Index");
                    }
                    else
                    {
                        return Page();
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace(response.RawContent);
                    this.telemetryClient.TrackEvent("SubscriptionNotDeleted");
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return Page();
        }
    }
}
