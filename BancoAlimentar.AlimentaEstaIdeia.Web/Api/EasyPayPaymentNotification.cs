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

        public async Task<IActionResult> PostAsync(TransactionNotificationRequest notif)
        {
            if (notif != null)
            {
                if (string.Equals(notif.Method, "MBW", StringComparison.OrdinalIgnoreCase))
                {
                    int donationId = this.context.Donation.CompleteMBWayPayment(
                        notif.Id.ToString(),
                        notif.Transaction.Key,
                        (float)notif.Transaction.Values.Requested,
                        (float)notif.Transaction.Values.Paid,
                        (float)notif.Transaction.Values.FixedFee,
                        (float)notif.Transaction.Values.VariableFee,
                        (float)notif.Transaction.Values.Tax,
                        (float)notif.Transaction.Values.Transfer);

                    await this.SendInvoiceEmail(donationId);
                }
                else if (string.Equals(notif.Method, "CC", StringComparison.OrdinalIgnoreCase))
                {
                    this.context.Donation.CompleteCreditCardPayment(
                        notif.Id.ToString(),
                        notif.Transaction.Key,
                        (float)notif.Transaction.Values.Requested,
                        (float)notif.Transaction.Values.Paid,
                        (float)notif.Transaction.Values.FixedFee,
                        (float)notif.Transaction.Values.VariableFee,
                        (float)notif.Transaction.Values.Tax,
                        (float)notif.Transaction.Values.Transfer);
                }

                return new JsonResult(new StatusDetails()
                {
                    Status = "ok",
                    Message = new Collection<string>() { "Alimenteestaideia: Payment Completed" },
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
