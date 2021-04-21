namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// This class represent the EasyPay base class used for the notification API.
    /// </summary>
    public class EasyPayControllerBase : ControllerBase
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IViewRenderService renderService;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayControllerBase"/> class.
        /// </summary>
        /// <param name="userManager">ASP.NET Core User Manager.</param>
        /// <param name="context">A reference to the <see cref="IUnitOfWork"/>.</param>
        /// <param name="configuration">Configuration system.</param>
        /// <param name="webHostEnvironment">A reference the <see cref="IWebHostEnvironment"/>.</param>
        /// <param name="renderService">This is the service to render in memory pages.</param>
        /// <param name="stringLocalizerFactory">A reference to the <see cref="IStringLocalizerFactory"/>.</param>
        public EasyPayControllerBase(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            IStringLocalizerFactory stringLocalizerFactory,
            TelemetryClient telemetryClient)
        {
            this.userManager = userManager;
            this.context = context;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.renderService = renderService;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Sends the invoice to the user.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        protected async Task SendInvoiceEmail(int donationId)
        {
            // send mail "Banco Alimentar: Confirmamos o pagamento da sua doação"
            // confirming that the multibank payment is processed.
            if (this.configuration.IsSendingEmailEnabled())
            {
                try
                {


                    Donation donation = this.context.Donation.GetFullDonationById(donationId);

                    if (donation == null) { 
                        this.telemetryClient.TrackException(new ExceptionTelemetry( new InvalidOperationException("SendInvoiceEmail donation not found for donation id={donationId}")));
                        return;
                    }

                    this.telemetryClient.TrackEvent("SendInvoiceEmail", new Dictionary<string, string> { { "DonationId", donationId.ToString() }, { "PublicId", donation.PublicId.ToString() } });

                    if (donation.WantsReceipt.HasValue && donation.WantsReceipt.Value)
                    {
                        this.telemetryClient.TrackEvent("SendInvoiceEmailWantsReceipt");
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
                        string.Concat(this.context.Invoice.GetInvoiceName(pdfFile.Item1), ".pdf"));
                    }
                    else
                    {
                        this.telemetryClient.TrackEvent("SendInvoiceEmailNoReceipt");
                        Mail.SendConfirmedPaymentMailToDonor(
                        this.configuration,
                        donation,
                        Path.Combine(
                            this.webHostEnvironment.WebRootPath,
                            this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path")));
                    }
                    this.telemetryClient.TrackEvent("SendInvoiceEmailComplete");
                }
                catch (Exception exc)
                {
                    this.telemetryClient.TrackException(new ExceptionTelemetry(new Exception("SendInvoiceEmail", exc)));
                }
            }
        }
    }
}
