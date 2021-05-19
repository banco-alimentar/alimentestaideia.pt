// -----------------------------------------------------------------------
// <copyright file="Delete.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Subscriptions
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    public class DeleteModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly EasyPayBuilder easyPayBuilder;
        private readonly TelemetryClient telemetryClient;

        public DeleteModel(
            IUnitOfWork context,
            IConfiguration configuration,
            EasyPayBuilder easyPayBuilder,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.configuration = configuration;
            this.easyPayBuilder = easyPayBuilder;
            this.telemetryClient = telemetryClient;
        }

        [BindProperty]
        public Subscription Subscription { get; set; }

        [BindProperty]
        public bool Error { get; set; }

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

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Subscription = this.context.SubscriptionRepository.GetById(id.Value);

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

            return Page();
        }
    }
}
