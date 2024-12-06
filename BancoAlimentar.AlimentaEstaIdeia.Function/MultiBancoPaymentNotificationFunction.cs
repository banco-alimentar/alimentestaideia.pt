// -----------------------------------------------------------------------
// <copyright file="MultiBancoPaymentNotificationFunction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Multibanco payment noficiation function.
    /// </summary>
    public class MultiBancoPaymentNotificationFunction
    {
        private static HttpClient client = new HttpClient();
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiBancoPaymentNotificationFunction"/> class.
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
            var config = FunctionInitializer.GetUnitOfWork(this.telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            string key = config.Configuration["ApiCertificateV3"];
            string notificationEndpoint = config.Configuration["WebUrl"];

            List<MultiBankPayment> all = context.PaymentNotificationRepository
                .GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications();

            foreach (var item in all)
            {
                WebUser user = applicationDbContext.Payments
                    .Include(p => p.Donation.User)
                    .Where(p => p.Id == item.Id)
                    .Select(p => p.Donation.User)
                    .FirstOrDefault();
                if (user != null)
                {
                    IOperationHolder<RequestTelemetry> requestTelemetry = this.telemetryClient.StartOperation<RequestTelemetry>("GET MultibancoNotification");
                    var response = await client.GetAsync(string.Format(notificationEndpoint, item.Id, key));
                    requestTelemetry.Telemetry.ResponseCode = response.StatusCode.ToString();
                    requestTelemetry.Telemetry.Success = response.IsSuccessStatusCode;
                    requestTelemetry.Telemetry.Url = response.RequestMessage.RequestUri;
                    this.telemetryClient.StopOperation(requestTelemetry);
                }
            }

            this.telemetryClient.TrackTrace($"There was {all.Count} elements to be proccesed.");
        }
    }
}
