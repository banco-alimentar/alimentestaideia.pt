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
    using System.Net.Http;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Azure.KeyVault;
    using Azure.Identity;
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
        private static HttpClient client;

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
        public async Task RunAsync([TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timer, ILogger log, CancellationToken token)
        {
            var config = FunctionInitializer.GetUnitOfWork(telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            if (client == null)
            {
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                KeyVaultClient keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        azureServiceTokenProvider.KeyVaultTokenCallback));

                CertificateBundle certificateBundle = await keyVaultClient.GetCertificateAsync(
                    config.configuration["VaultUri"],
                    "ApiCertificate",
                    token);
                HttpClientHandler handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = SslProtocols.Tls12;
                handler.ClientCertificates.Add(new X509Certificate2(certificateBundle.Cer));
                client = new HttpClient(handler);
            }



            List<MultiBankPayment> all = context.PaymentNotificationRepository
                .GetMultiBankPaymentsSinceLast24HoursWithoutEmailNotifications();

            foreach (var item in all)
            {
                WebUser user = applicationDbContext.PaymentItems
                    .Include(p => p.Donation.User)
                    .Where(p => p.Payment.Id == item.Id)
                    .Select(p => p.Donation.User)
                    .FirstOrDefault();
                if (user != null)
                {
                    var response = await client.GetAsync($"https://localhost:324/notifications/payment?multibankId={item.Id}");
                }
            }
        }
    }
}
