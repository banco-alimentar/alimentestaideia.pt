// -----------------------------------------------------------------------
// <copyright file="SubscriptionNotification.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.IO;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
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
    [Route("notifications/subcription")]
    [ApiController]
    public class SubscriptionNotification : Controller
    {
        private readonly IUnitOfWork context;
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IMail mail;
        private readonly IConfiguration configuration;
        private readonly TelemetryClient telemetryClient;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionNotification"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="applicationDbContext">Application Db Context.</param>
        /// <param name="mail">Mail.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        public SubscriptionNotification(
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
        public IActionResult Get(int subscriptionId, string key)
        {
            if (key == this.configuration["ApiCertificateV3"])
            {
                Subscription? subscription = applicationDbContext.Subscriptions
                    .Where(p => p.Id == subscriptionId)
                    .Include(p => p.User)
                    .FirstOrDefault();

                if (subscription != null)
                {
                    string body = Path.Combine(
                            this.webHostEnvironment.WebRootPath,
                            this.configuration.GetFilePath("Email.SubscriptionReminder.Body.Path"));

                    body = System.IO.File.ReadAllText(body);

                    if (mail.SendMail(
                            body,
                            this.configuration["Email.SubscriptionReminder.Subject"],
                            subscription.User.Email,
                            null,
                            null,
                            configuration))
                    {
                        context.SubscriptionNotificationRepository.AddNotification(subscription);
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
