// -----------------------------------------------------------------------
// <copyright file="Delete.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Client;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.FeatureManagement.Mvc;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Delete the subscription.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.SubscriptionAdmin)]
    public class DeleteModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly UserManager<WebUser> userManager;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work context.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        public DeleteModel(
            IUnitOfWork context,
            UserManager<WebUser> userManager,
            EasyPayBuilder easyPayBuilder,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.userManager = userManager;
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
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
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

            var user = await userManager.GetUserAsync(User);
            return Page();
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>Page.</returns>
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            Subscription = this.context.SubscriptionRepository.GetSubscriptionById(id.Value);
            if (user != null && userManager != null)
            {
                var subscriptionApi = this.easyPayBuilder
                        .GetSubscriptionPaymentApi();
                try
                {
                    var response = subscriptionApi
                        .SubscriptionIdDeleteWithHttpInfo(Guid.Parse(Subscription.EasyPaySubscriptionId));

                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
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
                catch (ApiException ex)
                {
                    JObject errorJson = JObject.Parse((string)ex.ErrorContent);
                    JArray errorMessages = (JArray)errorJson["message"];
                    string error = errorMessages.First.Value<string>();
                    if (error == "Resource Not Found")
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
                        this.telemetryClient.TrackException(ex);
                    }
                }
            }
            else
            {
                this.telemetryClient.TrackEvent(
                    "WhenDeletingSubscripionUserIsNotValid",
                    new Dictionary<string, string>()
                    {
                        { "CurrentLoggedUser", user?.Id },
                        { "SubcriptionId", Subscription?.Id.ToString() },
                    });
            }

            return Page();
        }
    }
}
