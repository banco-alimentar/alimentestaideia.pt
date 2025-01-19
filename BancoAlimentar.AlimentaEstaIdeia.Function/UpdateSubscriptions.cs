// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.WebJobs;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Update subscriptions Azure Function.
    /// </summary>
    public class UpdateSubscriptions
    {
        private readonly IServiceProvider serviceProvider;
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptions"/> class.
        /// </summary>
        public UpdateSubscriptions(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [Function("UpdateSubscriptions")]
        public async Task Run([TimerTrigger("* * */24 * * *")] TimerInfo timer, ILogger log)
        {
            InfrastructureDbContext infrastructureDbContext = this.serviceProvider.GetRequiredService<InfrastructureDbContext>();
            List<Tenant> allTenants = infrastructureDbContext.Tenants.ToList();
            IKeyVaultConfigurationManager keyVaultConfigurationManager = this.serviceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
            keyVaultConfigurationManager.LoadTenantConfiguration();
            foreach (var tenant in allTenants)
            {
                await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id, TenantDevelopmentOptions.ProductionOptions);
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

                using (IDbContextTransaction transaction = await applicationDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
                {
                    try
                    {
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        this.telemetryClient.TrackException(ex);
                        transaction.Rollback();
                    }
                    finally
                    {
                        transaction.Dispose();
                    }
                }
            }
        }
    }
}
