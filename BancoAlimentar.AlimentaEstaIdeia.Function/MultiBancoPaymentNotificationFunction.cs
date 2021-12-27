namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System.Linq;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.EntityFrameworkCore;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault.Models;

    /// <summary>
    /// Multibanco payment noficiation function.
    /// </summary>
    public class MultiBancoPaymentNotificationFunction
    {
        private TelemetryClient telemetryClient;
        private static HttpClient client = new HttpClient();

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
        /// <param name="token">Cancellation token.</param>
        [FunctionName("MultiBancoPaymentNotificationFunction")]
        public async Task RunAsync([TimerTrigger("0 59 11 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log, CancellationToken token)
        {
            var config = FunctionInitializer.GetUnitOfWork(telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            string key = config.configuration["ApiCertificateV3"];

            List<MultiBankPayment> all = context.PaymentNotificationRepository
                .GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications();

            foreach (var item in all)
            {
                WebUser user = applicationDbContext.PaymentItems
                    .Include(p => p.Donation.User)
                    .Where(p => p.Payment.Id == item.Id)
                    .Select(p => p.Donation.User)
                    .FirstOrDefault();
                if (user != null)
                {
                    var response = await client.GetAsync(string.Format(config.configuration["WebUrl"], item.Id, key));
                }
            }
        }
    }
}