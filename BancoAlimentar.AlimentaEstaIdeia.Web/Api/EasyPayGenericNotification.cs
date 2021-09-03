// -----------------------------------------------------------------------
// <copyright file="EasyPayGenericNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <summary>
    /// Easypay payment notification handler.
    /// </summary>
    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayGenericNotification"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Mail service.</param>
        public EasyPayGenericNotification(
            IUnitOfWork context,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            IMail mail)
            : base(context, configuration, telemetryClient, mail)
        {
            this.context = context;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="notificationRequest">Easypay transaction payment notification value.</param>
        /// <returns>A json with what we process.</returns>
        public async Task<IActionResult> PostAsync(GenericNotificationRequest notificationRequest)
        {
            int paymentId = 0;
            int donationId = 0;
            Collection<string> messages = new Collection<string>();
            if (notificationRequest != null)
            {
                if (notificationRequest.Type == GenericNotificationRequest.TypeEnum.SubscriptionCreate)
                {
                    int subscriptionId = this.context.SubscriptionRepository.CompleteSubcriptionCreate(
                        notificationRequest.Key,
                        notificationRequest.Status.Value);
                    messages.Add($"Subscription created {subscriptionId}");
                }
                else if (notificationRequest.Type == GenericNotificationRequest.TypeEnum.SubscriptionCapture)
                {
                    int subcriptionDonationId = this.context.SubscriptionRepository.SubscriptionCapture(
                        notificationRequest.Id.ToString(),
                        notificationRequest.Key,
                        notificationRequest.Status.Value,
                        DateTime.Parse(notificationRequest.Date));
                    messages.Add($"Subcription capture, new donation id {subcriptionDonationId}");
                }
                else
                {
                    paymentId = this.context.Donation.UpdatePaymentTransaction(
                    notificationRequest.Id.ToString(),
                    notificationRequest.Key,
                    notificationRequest.Status,
                    notificationRequest.Messages.FirstOrDefault());

                    messages.Add($"UpdatePaymentTransaction for paymentId {paymentId}");

                    donationId = this.context.Donation.CompleteMultiBankPayment(
                        notificationRequest.Id.ToString(),
                        notificationRequest.Key,
                        notificationRequest.Type.ToString(),
                        notificationRequest.Status.ToString(),
                        notificationRequest.Messages.FirstOrDefault());

                    if (donationId == -1)
                    {
                        donationId = this.context.Donation.GetDonationIdFromPaymentTransactionId(notificationRequest.Key);
                    }
                    else
                    {
                        messages.Add($"Multimanco payment completed for donation id {donationId}");
                    }

                    // Here is only place where we setn the invoice to the customer.
                    // After easypay notified us that the payment is correct.
                    await this.SendInvoiceEmail(donationId);
                    messages.Add($"Alimenteestaideia: Generic notification completed for payment id {paymentId}, multibanco donatino id {donationId} (it maybe null)");
                }
            }

            return new JsonResult(new StatusDetails()
            {
                Status = "ok",
                Message = messages,
            })
            {
                StatusCode = (int)HttpStatusCode.OK,
            };
        }
    }
}