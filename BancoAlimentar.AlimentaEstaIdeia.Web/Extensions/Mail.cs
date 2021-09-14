﻿// -----------------------------------------------------------------------
// <copyright file="Mail.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <inheritdoc/>
    public class Mail : IMail
    {
        private readonly IUnitOfWork context;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IFeatureManager featureManager;
        private readonly TelemetryClient telemetryClient;
        private readonly IWebHostEnvironment env;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mail"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="renderService">Render service.</param>
        /// <param name="webHostEnvironment">Web host environment.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="stringLocalizerFactory">Localization factory.</param>
        /// <param name="featureManager">Feature manager.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="env">Web host environemnt.</param>
        public Mail(
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            TelemetryClient telemetryClient,
            IWebHostEnvironment env)
        {
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.featureManager = featureManager;
            this.telemetryClient = telemetryClient;
            this.env = env;
        }

        /// <inheritdoc/>
        public async Task SendInvoiceEmail(Donation donation, HttpRequest request)
        {
            List<BasePayment> payments = this.context.Donation.GetPaymentsForDonation(donation.Id);
            var paymentIds = string.Join(',', payments.Select(p => p.Id.ToString()));

            if (donation.WantsReceipt.HasValue && donation.WantsReceipt.Value)
            {
                this.telemetryClient.TrackEvent("SendInvoiceEmailWantsReceipt", new Dictionary<string, string>() { { "DonationId", donation.Id.ToString() } });
                GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                    this.context,
                    this.renderService,
                    this.webHostEnvironment,
                    this.configuration,
                    this.stringLocalizerFactory,
                    this.featureManager,
                    this.env);

                (Invoice invoice, Stream pdfFile) = await generateInvoiceModel.GenerateInvoiceInternalAsync(donation.PublicId.ToString());
                SendConfirmedPaymentMailToDonor(
                this.configuration,
                donation,
                string.Join(',', this.context.Donation.GetPaymentsForDonation(donation.Id).Select(p => p.Id.ToString())),
                this.configuration["Email.ConfirmPaymentWithInvoice.Subject"],
                Path.Combine(
                    this.webHostEnvironment.WebRootPath,
                    this.configuration.GetFilePath("Email.ConfirmPaymentWithInvoice.Body.Path")),
                this.context.Donation.GetPaymentType(donation.ConfirmedPayment).ToString(),
                request,
                pdfFile,
                string.Concat(this.context.Invoice.GetInvoiceName(invoice), ".pdf"));
            }
            else
            {
                this.telemetryClient.TrackEvent("SendInvoiceEmailNoReceipt", new Dictionary<string, string>() { { "DonationId", donation.Id.ToString() } });
                SendConfirmedPaymentMailToDonor(
                    this.configuration,
                    donation,
                    string.Join(',', this.context.Donation.GetPaymentsForDonation(donation.Id).Select(p => p.Id.ToString())),
                    this.configuration["Email.ConfirmPaymentNoInvoice.Subject"],
                    Path.Combine(
                        this.webHostEnvironment.WebRootPath,
                        this.configuration.GetFilePath("Email.ConfirmPaymentNoInvoice.Body.Path")),
                    this.context.Donation.GetPaymentType(donation.ConfirmedPayment).ToString(),
                    request);
            }
        }

        /// <inheritdoc/>
        public bool SendMail(string body, string subject, string mailTo, Stream stream, string attachmentName, IConfiguration configuration)
        {
            if (!Convert.ToBoolean(configuration["IsEmailEnabled"]))
            {
                this.telemetryClient.TrackEvent("EmailIsNotEanbled");
                return true;
            }

            var client = new SmtpClient
            {
                Host = configuration["Smtp:Host"],
                Port = Convert.ToInt32(configuration["Smtp:Port"]),
            };

            bool useCredentials = Convert.ToBoolean(configuration["Smtp:UseCredentials"]);
            if (useCredentials)
            {
                var smtpUserInfo = new NetworkCredential(
                    configuration["Smtp:User"],
                    configuration["Smtp:Password"]);

                client.UseDefaultCredentials = false;
                client.Credentials = smtpUserInfo;
            }

            bool enableSsl = Convert.ToBoolean(configuration["Smtp:EnableSsl"]);
            client.EnableSsl = enableSsl;

            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            Attachment attachment = null;

            if (stream != null)
            {
                attachment = new Attachment(stream, attachmentName)
                {
                    ContentType = new ContentType("application/pdf; charset=UTF-8"),
                };
            }

            var message = new MailMessage
            {
                From = new MailAddress(configuration["EmailFrom"]),
                Body = body,
                Subject = subject,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };

            message.To.Add(new MailAddress(mailTo));
            if (attachment != null)
            {
                message.Attachments.Add(attachment);
            }

            try
            {
                client.Send(message);
                this.telemetryClient.TrackEvent("EmailSent");
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }

            return true;
        }

        /// <inheritdoc/>
        public bool SendMultibancoReferenceMailToDonor(IConfiguration configuration, Donation donation, string messageBodyPath)
        {
            string subject = configuration["Email.ReferenceToDonor.Subject"];
            string mailTo = donation.User.Email;

            if (File.Exists(messageBodyPath))
            {
                string mailBody = File.ReadAllText(messageBodyPath);
                string body = string.Format(mailBody, donation.ServiceEntity, donation.ServiceReference, donation.DonationAmount.ToString("F2", CultureInfo.GetCultureInfo("pt-PT")));
                return SendMail(body, subject, mailTo, null, null, configuration);
            }
            else
            {
                this.telemetryClient.TrackException(new FileNotFoundException("File not found", messageBodyPath));
                return false;
            }
        }

        private bool SendConfirmedPaymentMailToDonor(
            IConfiguration configuration,
            Donation donation,
            string paymentIds,
            string subject,
            string messageBodyPath,
            string paymentSystem,
            HttpRequest request,
            Stream stream = null,
            string attachmentName = null)
        {
            string mailTo = donation.User.Email;

            if (File.Exists(messageBodyPath))
            {
                string mailBody = File.ReadAllText(messageBodyPath);
                mailBody = mailBody.Replace("{donationId}", donation.Id.ToString());
                mailBody = mailBody.Replace("{paymentId}", paymentIds);
                mailBody = mailBody.Replace("{PaymentSystem}", paymentSystem);
                mailBody = mailBody.Replace("{publicDonationId}", donation.PublicId.ToString());
                mailBody = mailBody.Replace("{Scheme}", request.Scheme);
                mailBody = mailBody.Replace("{Host}", request.Host.Value);
                return SendMail(mailBody, subject, mailTo, stream, attachmentName, configuration);
            }
            else
            {
                this.telemetryClient.TrackException(new FileNotFoundException("File not found", messageBodyPath));
                return false;
            }
        }
    }
}
