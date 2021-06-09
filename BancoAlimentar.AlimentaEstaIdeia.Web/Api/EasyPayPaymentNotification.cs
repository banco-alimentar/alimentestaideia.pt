// -----------------------------------------------------------------------
// <copyright file="EasyPayPaymentNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
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

    [Route("easypay/payment")]
    [ApiController]
    public class EasyPayPaymentNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;

        public EasyPayPaymentNotification(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            IStringLocalizerFactory stringLocalizerFactory,
            TelemetryClient telemetryClient)
            : base(
                  context,
                  configuration,
                  webHostEnvironment,
                  renderService,
                  stringLocalizerFactory,
                  telemetryClient)
        {
            this.context = context;
            this.telemetryClient = telemetryClient;
        }

        public async Task<IActionResult> PostAsync(TransactionNotificationRequest value)
        {
            if (value != null)
            {
                int donationId = 0;
                if (string.Equals(value.Method, "MBW", StringComparison.OrdinalIgnoreCase))
                {
                    donationId = this.context.Donation.CompleteMBWayPayment(
                        value.Id.ToString(),
                        value.Transaction.Key,
                        (float)value.Transaction.Values.Requested,
                        (float)value.Transaction.Values.Paid,
                        (float)value.Transaction.Values.FixedFee,
                        (float)value.Transaction.Values.VariableFee,
                        (float)value.Transaction.Values.Tax,
                        (float)value.Transaction.Values.Transfer);
                }
                else if (string.Equals(value.Method, "CC", StringComparison.OrdinalIgnoreCase))
                {
                    donationId = this.context.Donation.CompleteCreditCardPayment(
                        value.Id.ToString(),
                        value.Transaction.Key,
                        (float)value.Transaction.Values.Requested,
                        (float)value.Transaction.Values.Paid,
                        (float)value.Transaction.Values.FixedFee,
                        (float)value.Transaction.Values.VariableFee,
                        (float)value.Transaction.Values.Tax,
                        (float)value.Transaction.Values.Transfer);
                }

                return new JsonResult(new StatusDetails()
                {
                    Status = "ok",
                    Message = new Collection<string>() { $"Alimenteestaideia: Payment Completed for donation {donationId}" },
                })
                { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new StatusDetails() { Status = "not found" }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
