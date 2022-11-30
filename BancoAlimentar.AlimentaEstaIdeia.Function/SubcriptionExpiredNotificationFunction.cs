namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Subscription expiration notification function.
    /// </summary>
    public class SubcriptionExpiredNotificationFunction
    {
        private TelemetryClient telemetryClient;
        private static HttpClient client = new HttpClient();

        /// <summary>
        /// Default constructor for <see cref="MultiBancoPaymentNotificationFunction"/>.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration.</param>
        public SubcriptionExpiredNotificationFunction(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        /// <param name="token">Cancellation token.</param>
        [FunctionName("SubcriptionNotificationFunction")]
        public async Task RunAsync([TimerTrigger("0 59 11 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log, CancellationToken token)
        {
            var config = FunctionInitializer.GetUnitOfWork(telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            string key = config.configuration["ApiCertificateV3"];
            string notificationEndpoint = config.configuration["WebUrl"];

            List<Subscription> expiringSubscription =
                context.SubscriptionRepository.GetSubscriptionExpiringBy(DateTime.UtcNow.AddMonths(1));

            foreach (var item in expiringSubscription)
            {
                if (!context.SubscriptionNotificationRepository.FindNotificationForSubscription(item.Id))
                {
                    // send email

                    IOperationHolder<RequestTelemetry> requestTelemetry = this.telemetryClient.StartOperation<RequestTelemetry>("GET SubcriptionNotification");
                    var response = await client.GetAsync(string.Format(notificationEndpoint, item.Id, key));
                    requestTelemetry.Telemetry.ResponseCode = response.StatusCode.ToString();
                    requestTelemetry.Telemetry.Success = response.IsSuccessStatusCode;
                    requestTelemetry.Telemetry.Url = response.RequestMessage.RequestUri;
                    this.telemetryClient.StopOperation(requestTelemetry);
                }
            }
        }
    }
}
