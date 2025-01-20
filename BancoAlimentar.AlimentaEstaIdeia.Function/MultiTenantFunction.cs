// -----------------------------------------------------------------------
// <copyright file="MultiTenantFunction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Base class for all Azure Functions.
    /// </summary>
    public class MultiTenantFunction
    {
        private readonly IServiceProvider serviceProvider;
        private TelemetryClient telemetryClient;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTenantFunction"/> class.
        /// </summary>
        public MultiTenantFunction(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets or sets the function to execute.
        /// </summary>
        public Func<IUnitOfWork, ApplicationDbContext, Task> ExecuteFunction { get; set; }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        public IServiceProvider ServiceProvider => this.serviceProvider;

        /// <summary>
        /// Gets the telemetry client.
        /// </summary>
        public TelemetryClient TelemetryClient => this.telemetryClient;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration => this.configuration;

        /// <summary>
        /// Load the configuration for all tenants.
        /// </summary>
        /// <returns>A task object to monitor progress.</returns>
        public async Task RunFunctionCore()
        {
            InfrastructureDbContext infrastructureDbContext = this.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            List<Tenant> allTenants = infrastructureDbContext.Tenants.ToList();
            IKeyVaultConfigurationManager keyVaultConfigurationManager = this.ServiceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
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
                this.configuration = configurationBuilder.Build();
                var config = FunctionInitializer.GetUnitOfWork(this.TelemetryClient, this.configuration);
                try
                {
                    await this.ExecuteFunction(config.UnitOfWork, config.ApplicationDbContext);
                }
                catch (Exception ex)
                {
                    this.TelemetryClient.TrackException(ex);
                }
            }
        }
    }
}
