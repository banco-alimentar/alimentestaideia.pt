// -----------------------------------------------------------------------
// <copyright file="EasyPayGenericNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;

        public EasyPayGenericNotification(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            IStringLocalizerFactory stringLocalizerFactory,
            TelemetryClient telemetryClient,
            IFeatureManager featureManager)
            : base(
                  context,
                  configuration,
                  webHostEnvironment,
                  renderService,
                  stringLocalizerFactory,
                  telemetryClient,
                  featureManager)
        {
            this.context = context;
            this.telemetryClient = telemetryClient;
        }

        public async Task<IActionResult> PostAsync(GenericNotificationRequest notificationRequest)
        {
            int paymentId = 0;
            int donationId = 0;
            if (notificationRequest != null)
            {
                paymentId = this.context.Donation.UpdatePaymentTransaction(
                    notificationRequest.Id.ToString(),
                    notificationRequest.Key,
                    notificationRequest.Status,
                    notificationRequest.Messages.FirstOrDefault());

                if (notificationRequest.Type == GenericNotificationRequest.TypeEnum.SubscriptionCreate)
                {
                    this.context.SubscriptionRepository.CompleteSubcriptionCreate(
                        notificationRequest.Key,
                        notificationRequest.Status.Value);
                }
                else
                {
                    donationId = this.context.Donation.CompleteMultiBankPayment(
                        notificationRequest.Id.ToString(),
                        notificationRequest.Key,
                        notificationRequest.Type.ToString(),
                        notificationRequest.Status.ToString(),
                        notificationRequest.Messages.FirstOrDefault());

                    if (donationId != -1)
                    {
                        // We only sent the invoice email only when the payment is MultiBanco
                        await this.SendInvoiceEmail(donationId);
                    }
                }
            }

            return new JsonResult(new StatusDetails()
            {
                Status = "ok",
                Message = new Collection<string>() { $"Alimenteestaideia: Generic notification completed for payment id {paymentId}, multibanco donatino id {donationId} (it maybe null)" },
            })
            { StatusCode = (int)HttpStatusCode.OK };
        }
    }
}