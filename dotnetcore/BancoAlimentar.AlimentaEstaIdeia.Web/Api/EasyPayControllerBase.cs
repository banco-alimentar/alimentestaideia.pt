namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class EasyPayControllerBase : ControllerBase
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IViewRenderService renderService;
        private readonly IStringLocalizerFactory stringLocalizerFactory;

        public EasyPayControllerBase(
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

        protected async Task SendInvoiceEmail(int donationId)
        {
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
                    donation,
                    Path.Combine(
                        this.webHostEnvironment.WebRootPath,
                        this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path")),
                    pdfFile.Item2,
                    $"RECIBO Nº {pdfFile.Item1.Number}.pdf");
                }
                else
                {
                    Mail.SendConfirmedPaymentMailToDonor(
                    this.configuration,
                    donation,
                    Path.Combine(
                        this.webHostEnvironment.WebRootPath,
                        this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path")));
                }
            }
        }
    }
}
