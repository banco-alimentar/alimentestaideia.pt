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
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Multibanco payment noficiation function.
    /// </summary>
    public class MultiBancoPaymentNotificationFunction : MultiTenantFunction
    {
        private HttpClient client = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiBancoPaymentNotificationFunction"/> class.
        /// </summary>
        public MultiBancoPaymentNotificationFunction(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
            : base(telemetryConfiguration, serviceProvider)
        {
            this.ExecuteFunction = new Func<IUnitOfWork, ApplicationDbContext, Task>(this.UpdateSubscriptionsFunction);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        /// <param name="token">Cancellation token.</param>
        [Function("MultiBancoPaymentNotificationFunction")]
        public async Task RunAsync([TimerTrigger("0 59 11 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log, CancellationToken token)
        {
            await this.RunFunctionCore();
        }

        private async Task UpdateSubscriptionsFunction(IUnitOfWork context, ApplicationDbContext applicationDbContext)
        {
            string key = this.Configuration["ApiCertificateV3"];
            string notificationEndpoint = this.Configuration["WebUrl"];

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
                    IOperationHolder<RequestTelemetry> requestTelemetry = this.TelemetryClient.StartOperation<RequestTelemetry>("GET MultibancoNotification");
                    var response = await this.client.GetAsync(string.Format(notificationEndpoint, item.Id, key));
                    requestTelemetry.Telemetry.ResponseCode = response.StatusCode.ToString();
                    requestTelemetry.Telemetry.Success = response.IsSuccessStatusCode;
                    requestTelemetry.Telemetry.Url = response.RequestMessage.RequestUri;
                    this.TelemetryClient.StopOperation(requestTelemetry);
                }
            }

            this.TelemetryClient.TrackTrace($"There was {all.Count} elements to be proccesed.");
        }
    }
}
