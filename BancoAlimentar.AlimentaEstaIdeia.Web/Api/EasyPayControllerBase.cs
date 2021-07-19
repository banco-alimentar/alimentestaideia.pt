// -----------------------------------------------------------------------
// <copyright file="EasyPayControllerBase.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <summary>
    /// This class represent the EasyPay base class used for the notification API.
    /// </summary>
    public class EasyPayControllerBase : ControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IViewRenderService renderService;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly TelemetryClient telemetryClient;
        private readonly IFeatureManager featureManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyPayControllerBase"/> class.
        /// </summary>
        /// <param name="context">A reference to the <see cref="IUnitOfWork"/>.</param>
        /// <param name="configuration">Configuration system.</param>
        /// <param name="webHostEnvironment">A reference the <see cref="IWebHostEnvironment"/>.</param>
        /// <param name="renderService">This is the service to render in memory pages.</param>
        /// <param name="stringLocalizerFactory">A reference to the <see cref="IStringLocalizerFactory"/>.</param>
        /// <param name="telemetryClient">A reference to the <see cref="TelemetryClient"/>.</param>
        /// <param name="featureManager">A reference to the Feature Manager</param>
        public EasyPayControllerBase(
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            IStringLocalizerFactory stringLocalizerFactory,
            TelemetryClient telemetryClient,
            IFeatureManager featureManager)
        {
            this.context = context;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.renderService = renderService;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.telemetryClient = telemetryClient;
            this.featureManager = featureManager;
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
                    if (donationId > 0)
                    {
                        Donation donation = this.context.Donation.GetFullDonationById(donationId);

                        if (donation == null)
                        {
                            EventTelemetry donationNotFound = new EventTelemetry("DonationNotFound");
                            donationNotFound.Properties.Add("DonationId", donationId.ToString());
                            donationNotFound.Properties.Add("Method", string.Concat(GetType().Name, ".", nameof(SendInvoiceEmail)));
                            this.telemetryClient.TrackEvent(donationNotFound);
                            return;
                        }

                        this.telemetryClient.TrackEvent("SendInvoiceEmail", new Dictionary<string, string> { { "DonationId", donationId.ToString() }, { "PublicId", donation.PublicId.ToString() } });

                        if (donation.WantsReceipt.HasValue && donation.WantsReceipt.Value)
                        {
                            this.telemetryClient.TrackEvent("SendInvoiceEmailWantsReceipt");
                            GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                                this.context,
                                this.renderService,
                                this.webHostEnvironment,
                                this.configuration,
                                this.stringLocalizerFactory,
                                this.featureManager);

                            Tuple<Invoice, Stream> pdfFile = await generateInvoiceModel.GenerateInvoiceInternalAsync(donation.PublicId.ToString());
                            Mail.SendConfirmedPaymentMailToDonor(
                            this.configuration,
                            donation,
                            string.Join(',', this.context.Donation.GetPaymentsForDonation(donation.Id).Select(p => p.Id.ToString())),
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
                            string.Join(',', this.context.Donation.GetPaymentsForDonation(donation.Id).Select(p => p.Id.ToString())),
                            Path.Combine(
                                this.webHostEnvironment.WebRootPath,
                                this.configuration.GetFilePath("Email.ConfirmedPaymentMailToDonor.Body.Path")));
                        }

                        this.telemetryClient.TrackEvent("SendInvoiceEmailComplete");
                    }
                    else
                    {
                        EventTelemetry donationNotFound = new EventTelemetry("DonationNotFound");
                        donationNotFound.Properties.Add("DonationId", donationId.ToString());
                        donationNotFound.Properties.Add("Method", string.Concat(GetType().Name, ".", nameof(SendInvoiceEmail)));
                        this.telemetryClient.TrackEvent(donationNotFound);
                    }
                }
                catch (Exception exc)
                {
                    this.telemetryClient.TrackException(exc);
                }
            }
        }
    }
}
