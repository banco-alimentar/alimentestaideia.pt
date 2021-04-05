namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : ControllerBase
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IViewRenderService renderService;
        private readonly IStringLocalizerFactory stringLocalizerFactory;

        public EasyPayGenericNotification(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.userManager = userManager;
            this.context = context;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.renderService = renderService;
            this.stringLocalizerFactory = stringLocalizerFactory;
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

                // send mail "Banco Alimentar: Confirmamos o pagamento da sua doação"
                // confirming that the multibank payment is processed.
                if (this.configuration.IsSendingEmailEnabled())
                {
                    Donation donation = this.context.Donation.GetFullDonationById(donationId);

                    if (donation.WantsReceipt.HasValue && donation.WantsReceipt.Value)
                    {
                        GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                            this.userManager,
                            this.context,
                            this.renderService,
                            this.webHostEnvironment,
                            this.configuration,
                            this.stringLocalizerFactory);

                        Tuple<Invoice, Stream> pdfFile = await generateInvoiceModel.GenerateInvoiceInternalAsync(donation.PublicId.ToString());
                        Mail.SendConfirmedPaymentMailToDonor(
                        this.configuration,
                        this.context.Donation.GetFullDonationById(donationId),
                        Path.Combine(
                            this.webHostEnvironment.WebRootPath,
                            this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path")),
                        pdfFile.Item2,
                        $"RECIBO Nº B{DateTime.Now.Year}-{pdfFile.Item1.Id}.pdf");
                    }
                    else
                    {
                        Mail.SendConfirmedPaymentMailToDonor(
                        this.configuration,
                        this.context.Donation.GetFullDonationById(donationId),
                        Path.Combine(
                            this.webHostEnvironment.WebRootPath,
                            this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path")));
                    }
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
