// -----------------------------------------------------------------------
// <copyright file="TenantStaticSyncHostedService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Synchronize the tenant static files to the AppService local hard drive.
    /// </summary>
    public class TenantStaticSyncHostedService : BackgroundService
    {
        private readonly ILogger<TenantStaticSyncHostedService> logger;
        private readonly IServiceProvider services;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticSyncHostedService"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="services">Service provider.</param>
        public TenantStaticSyncHostedService(
            ILogger<TenantStaticSyncHostedService> logger,
            IServiceProvider services)
        {
            this.logger = logger;
            this.services = services;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = this.services.CreateScope();
            IWebHostEnvironment webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            IKeyVaultConfigurationManager keyVaultConfigurationManager = scope.ServiceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
            ITenantStaticLocalCacheService localCacheService = scope.ServiceProvider.GetRequiredService<ITenantStaticLocalCacheService>();
            bool configurationLoaded = keyVaultConfigurationManager.LoadTenantConfiguration();
            if (!configurationLoaded)
            {
                return;
            }

            using IInfrastructureUnitOfWork infrastructureUnitOfWork = scope.ServiceProvider.GetRequiredService<IInfrastructureUnitOfWork>();
            List<Model.Tenant> allTenants = infrastructureUnitOfWork
                .TenantRepository
                .GetAllTenantForEnvironment(webHostEnvironment.EnvironmentName);

            foreach (Model.Tenant tenant in allTenants)
            {
                try
                {
                    await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id, TenantDevelopmentOptions.ProductionOptions);
                    var configuration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
                    string? connectionString = configuration?["AzureStorage:ConnectionString"];
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        continue;
                    }

                    await localCacheService.ResyncFromBlobAsync(
                        tenant.PublicId,
                        tenant.NormalizedName,
                        connectionString,
                        onlyIfSizeChanged: true,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Exception for tenant {TenantId} | {TenantName}", tenant.Id, tenant.NormalizedName);
                }
            }
        }
    }
}
