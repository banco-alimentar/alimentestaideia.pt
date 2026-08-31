// -----------------------------------------------------------------------
// <copyright file="PaymentNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Payment notification API.
    /// </summary>
    [Route("notifications/payment")]
    [ApiController]
    public class PaymentNotification : ControllerBase
    {
        private readonly IUnitOfWork context;
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IMail mail;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentNotification"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="applicationDbContext">Application Db Context.</param>
        /// <param name="mail">Mail.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        public PaymentNotification(
            IUnitOfWork context,
            ApplicationDbContext applicationDbContext,
            IMail mail,
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.applicationDbContext = applicationDbContext;
            this.mail = mail;
            this.configuration = configuration;
            this.telemetryClient = telemetryClient;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Get operation.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public IActionResult Get(int multibankId, string key)
        {
            if (key == this.configuration["ApiCertificateV3"])
            {
                MultiBankPayment multibanco = applicationDbContext.MultiBankPayments
                    .Include(p => p.Donation)
                    .ThenInclude(d => d.User)
                    .Include(p => p.Donation)
                    .ThenInclude(d => d.FoodBank)
                    .Where(p => p.Id == multibankId)
                    .FirstOrDefault();
                WebUser user = multibanco?.Donation?.User;
                if (user != null &&
                    multibanco != null &&
                    DonationPaymentCompletion.IsAwaitingMultiBankPayment(multibanco.Donation, multibanco) &&
                    !this.context.PaymentNotificationRepository.EmailNotificationExits(multibankId))
                {
                    string body = Path.Combine(
                            this.webHostEnvironment.WebRootPath,
                            this.configuration.GetFilePath("Email.MultibancoReminder.Body.Path"));

                    body = System.IO.File.ReadAllText(body);
                    body = this.ReplaceReminderDetails(body, multibanco);

                    if (mail.SendMail(
                            body,
                            this.configuration["Email.MultibancoReminder.Subject"],
                            user.Email,
                            null,
                            null,
                            configuration))
                    {
                        context.PaymentNotificationRepository.AddEmailNotification(
                            user,
                            multibanco);
                    }
                }

                return this.Ok();
            }
            else
            {
                return this.Forbid();
            }
        }

        private string ReplaceReminderDetails(string body, MultiBankPayment multibanco)
        {
            Donation donation = multibanco.Donation;
            CultureInfo portugueseCulture = CultureInfo.GetCultureInfo("pt-PT");
            string donationAmount = donation.DonationAmount.ToString("F2", portugueseCulture);
            string paymentAmount = (multibanco.Requested > 0 ? multibanco.Requested : (float)donation.DonationAmount)
                .ToString("F2", portugueseCulture);
            string donationDate = donation.DonationDate.ToString("dd/MM/yyyy HH:mm", portugueseCulture);
            string donationDetailsUrl = $"{this.Request.Scheme}://{this.Request.Host}/Payment?publicId={Uri.EscapeDataString(donation.PublicId.ToString())}";

            return body
                .Replace("{donationDate}", WebUtility.HtmlEncode(donationDate), StringComparison.Ordinal)
                .Replace("{donationAmount}", WebUtility.HtmlEncode(donationAmount), StringComparison.Ordinal)
                .Replace("{foodBank}", WebUtility.HtmlEncode(donation.FoodBank?.Name ?? string.Empty), StringComparison.Ordinal)
                .Replace("{serviceEntity}", WebUtility.HtmlEncode(donation.ServiceEntity ?? string.Empty), StringComparison.Ordinal)
                .Replace("{serviceReference}", WebUtility.HtmlEncode(donation.ServiceReference ?? string.Empty), StringComparison.Ordinal)
                .Replace("{paymentAmount}", WebUtility.HtmlEncode(paymentAmount), StringComparison.Ordinal)
                .Replace("{donationDetailsUrl}", WebUtility.HtmlEncode(donationDetailsUrl), StringComparison.Ordinal);
        }
    }
}
