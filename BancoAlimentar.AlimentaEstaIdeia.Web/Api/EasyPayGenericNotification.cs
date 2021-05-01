namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;

        public EasyPayGenericNotification(
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

        public async Task<IActionResult> PostAsync(GenericNotificationRequest notificationRequest)
        {
            if (notificationRequest != null)
            {
                int donationId = this.context.Donation.CompleteMultiBankPayment(
                    notificationRequest.Id.ToString(),
                    notificationRequest.Key,
                    notificationRequest.Type.ToString(),
                    notificationRequest.Status.ToString(),
                    notificationRequest.Messages.FirstOrDefault());

                await this.SendInvoiceEmail(donationId);

                return new JsonResult(new StatusDetails()
                {
                    Status = "ok",
                    Message = new Collection<string>() { "Alimenteestaideia: Payment Completed" },
                })
                { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new StatusDetails()
                {
                    Status = "not found",
                    Message = new Collection<string>() { "Alimenteestaideia: Easypay Generic notification not provided" },
                })
                { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
