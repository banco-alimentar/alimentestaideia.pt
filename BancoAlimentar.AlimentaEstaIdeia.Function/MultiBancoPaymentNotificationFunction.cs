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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.WebJobs;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Multibanco payment noficiation function.
    /// </summary>
    public class MultiBancoPaymentNotificationFunction
    {
        private static HttpClient client = new HttpClient();
        private readonly IServiceProvider serviceProvider;
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiBancoPaymentNotificationFunction"/> class.
        /// </summary>
        public MultiBancoPaymentNotificationFunction(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.serviceProvider = serviceProvider;
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
            InfrastructureDbContext infrastructureDbContext = this.serviceProvider.GetRequiredService<InfrastructureDbContext>();
            List<Tenant> allTenants = infrastructureDbContext.Tenants.ToList();
            IKeyVaultConfigurationManager keyVaultConfigurationManager = this.serviceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
            keyVaultConfigurationManager.LoadTenantConfiguration();
            foreach (var tenant in allTenants)
            {
                bool loaded = await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id, TenantDevelopmentOptions.ProductionOptions);
                if (loaded)
                {
                    Dictionary<string, string> tenantConfiguration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
                    MemoryConfigurationSource memoryConfigurationSource = new MemoryConfigurationSource
                    {
                        InitialData = tenantConfiguration,
                    };
                    ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                    configurationBuilder.Add(memoryConfigurationSource);
                    IConfigurationRoot configuration = configurationBuilder.Build();
                    var config = FunctionInitializer.GetUnitOfWork(this.telemetryClient, configuration);
                    IUnitOfWork context = config.UnitOfWork;
                    ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

                    string key = configuration["ApiCertificateV3"];
                    string notificationEndpoint = configuration["WebUrl"];

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
    }
}
