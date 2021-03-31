namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.Net;
    using System.Linq;
    using System.Collections.ObjectModel;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using System.IO;
    using System;

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

        public IActionResult Post(GenericNotificationRequest notif)
        {
            if (notif != null)
            {
                this.context.Donation.CompleteMultiBankPayment(
                    notif.Id.ToString(),
                    notif.Key,
                    notif.Type.ToString(),
                    notif.Status.ToString(),
                    notif.Messages.FirstOrDefault());

                // send mail "Banco Alimentar: Confirmamos o pagamento da sua doação"
                // confirming that the multibank payment is processed.
                if (this.configuration.IsSendingEmailEnabled())
                {
                    int donationPublicId = this.context.Donation.GetDonationIdFromPublicId(new Guid(notif.Key));
                    Mail.SendConfirmedPaymentMailToDonor(
                        this.configuration, 
                        this.context.Donation.GetFullDonationById(donationPublicId), 
                        Path.Combine(this.webHostEnvironment.WebRootPath, this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path"))
                        );
                }


                return new JsonResult(new StatusDetails() {
                    Status = "ok",
                    Message = new Collection<string>() { "Alimenteestaideia: Payment Completed" },
                }) { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new StatusDetails() {
                    Status = "not found",
                    Message = new Collection<string>() { "Alimenteestaideia: Easypay Generic notification not provided" },
                }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
