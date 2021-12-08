namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.EntityFrameworkCore;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Multibanco payment noficiation function.
    /// </summary>
    public class MultiBancoPaymentNotificationFunction
    {
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Default constructor for <see cref="MultiBancoPaymentNotificationFunction"/>.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration.</param>
        public MultiBancoPaymentNotificationFunction(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [FunctionName("MultiBancoPaymentNotificationFunction")]
        public void Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            var config = FunctionInitializer.GetUnitOfWork(telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            List<MultiBankPayment> all = context.PaymentNotificationRepository
                .GetMultiBankPaymentsSinceLast24HoursWithoutEmailNotifications();

            ServiceProvider serviceProvider = FunctionInitializer.GetServiceCollection(config.configuration, telemetryClient);
            IMail mail = serviceProvider.GetRequiredService<IMail>();

            foreach (var item in all)
            {
                WebUser user = applicationDbContext.PaymentItems
                    .Include(p => p.Donation.User)
                    .Where(p => p.Payment.Id == item.Id)
                    .Select(p => p.Donation.User)
                    .FirstOrDefault();
                if (user != null)
                {
                    if (mail.SendMail(
                        "Multibanco",
                        "Reminder for the multibanco",
                        user.Email,
                        null,
                        null,
                        config.configuration))
                    {
                        context.PaymentNotificationRepository.AddEmailNotification(
                            user,
                            item);
                    }
                }
            }
        }
    }
}
