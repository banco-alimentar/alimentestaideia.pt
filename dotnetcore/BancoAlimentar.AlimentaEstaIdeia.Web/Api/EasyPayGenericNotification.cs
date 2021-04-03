namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;

    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : ControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;

        public EasyPayGenericNotification(
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Post(GenericNotificationRequest notificationRequest)
        {
            if (notificationRequest != null)
            {
                int donationId = this.context.Donation.CompleteMultiBankPayment(
                    notificationRequest.Id.ToString(),
                    notificationRequest.Key,
                    notificationRequest.Type.ToString(),
                    notificationRequest.Status.ToString(),
                    notificationRequest.Messages.FirstOrDefault());

                // send mail "Banco Alimentar: Confirmamos o pagamento da sua doação"
                // confirming that the multibank payment is processed.
                if (this.configuration.IsSendingEmailEnabled())
                {
                    Mail.SendConfirmedPaymentMailToDonor(
                        this.configuration,
                        this.context.Donation.GetFullDonationById(donationId),
                        Path.Combine(this.webHostEnvironment.WebRootPath, this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path"))
                        );
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
