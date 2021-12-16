// -----------------------------------------------------------------------
// <copyright file="PaymentNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.IO;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
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
                    .Where(p => p.Id == multibankId)
                    .FirstOrDefault();
                WebUser user = applicationDbContext.PaymentItems
                        .Include(p => p.Donation.User)
                        .Where(p => p.Payment.Id == multibankId)
                        .Select(p => p.Donation.User)
                        .FirstOrDefault();
                if (user != null)
                {
                    string body = Path.Combine(
                            this.webHostEnvironment.WebRootPath,
                            this.configuration.GetFilePath("Email.MultibancoReminder.Body.Path"));

                    body = System.IO.File.ReadAllText(body);

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
    }
}
