// -----------------------------------------------------------------------
// <copyright file="Thanks.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    public class ThanksModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IViewRenderService renderService;
        private readonly TelemetryClient telemetryClient;
        private readonly IFeatureManager featureManager;
        private readonly IStringLocalizer localizer;

        public ThanksModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IViewLocalizer localizer,
            IHtmlLocalizer<ThanksModel> html,
            IStringLocalizerFactory stringLocalizerFactory,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            TelemetryClient telemetryClient,
            IFeatureManager featureManager)
        {
            this.userManager = userManager;
            this.context = context;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.renderService = renderService;
            this.telemetryClient = telemetryClient;
            this.featureManager = featureManager;
            this.localizer = stringLocalizerFactory.Create("Pages.Thanks", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
        }

        public WebUser CurrentUser { get; set; }

        public Donation Donation { get; set; }

        [BindProperty]
        public string TwittMessage { get; set; }

        public static void CompleteDonationFlow(HttpContext context)
        {
            if (context != null)
            {
                context.Items.Remove(DonationFlowTelemetryInitializer.DonationSessionKey);
                context.Session.Remove(DonationFlowTelemetryInitializer.DonationSessionKey);
            }
        }

        public async Task OnGet(int id)
        {
#if RELEASE
            id = 0;
#endif

            if (TempData["Donation"] != null)
            {
                id = (int)TempData["Donation"];
            }

            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));

            Donation = this.context.Donation.GetFullDonationById(id);
            if (Donation != null)
            {
                this.context.Donation.InvalidateTotalCache();
                string foodBank = "Lisbon";
                if (Donation.FoodBank != null && !string.IsNullOrEmpty(Donation.FoodBank.Name))
                {
                    foodBank = Donation.FoodBank.Name;
                }

                TwittMessage = string.Format(localizer.GetString("TwittMessage"), Donation.DonationAmount, foodBank);
                if (this.configuration.IsSendingEmailEnabled())
                {
                    List<BasePayment> payments = this.context.Donation.GetPaymentsForDonation(id);
                    var paymentIds = string.Join(',', payments.Select(p => p.Id.ToString()));
                    await SendThanksEmail(Donation.User.Email, Donation.PublicId.ToString(), Donation, paymentIds);
                }

                this.telemetryClient.TrackEvent("ThanksOnGetSuccess", new Dictionary<string, string> { { "DonationId", id.ToString() }, { "UserId", CurrentUser?.Id }, { "PublicId", Donation.PublicId.ToString() } });
            }
            else
            {
                this.TrackExceptionTelemetry("Thanks.OnGet donation is null", id, CurrentUser?.Id);
            }

            CompleteDonationFlow(HttpContext);
        }

        public async Task SendThanksEmail(string email, string publicDonationId, Donation donation, string paymentsId)
        {
            string bodyFilePath = Path.Combine(this.webHostEnvironment.WebRootPath, this.configuration.GetFilePath("Email.PaymentToDonor.Body.Path"));
            string html = System.IO.File.ReadAllText(bodyFilePath);
            html = html.Replace("{publicDonationId}", publicDonationId);
            html = html.Replace("{donationId}", donation.Id.ToString());
            html = html.Replace("{paymentId}", paymentsId);
            if (donation.WantsReceipt.HasValue && donation.WantsReceipt.Value)
            {
                GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                    this.context,
                    this.renderService,
                    this.webHostEnvironment,
                    this.configuration,
                    this.stringLocalizerFactory,
                    this.featureManager);

                Tuple<Invoice, Stream> pdfFile = await generateInvoiceModel.GenerateInvoiceInternalAsync(donation.PublicId.ToString());
                Mail.SendMail(
                    html,
                    this.configuration["Email.PaymentToDonor.Subject"],
                    email,
                    pdfFile.Item2,
                    $"RECIBO Nº {pdfFile.Item1.Number}.pdf",
                    this.configuration);
            }
            else
            {
                Mail.SendMail(html, this.configuration["Email.PaymentToDonor.Subject"], email, null, null, this.configuration);
            }
        }

        /// <summary>
        /// Tracks an ExceptionTelemetry to App Insights.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="donationId">The donation id that it refers to.</param>
        /// <param name="userId">The userId that was passed to the method.</param>
        private void TrackExceptionTelemetry(string message, int donationId, string userId)
        {
            ExceptionTelemetry exceptionTelemetry = new ExceptionTelemetry(new InvalidOperationException(message));
            exceptionTelemetry.Properties.Add("DonationId", donationId.ToString());
            exceptionTelemetry.Properties.Add("UserId", userId);
            this.telemetryClient.TrackException(exceptionTelemetry);
        }
    }
}
