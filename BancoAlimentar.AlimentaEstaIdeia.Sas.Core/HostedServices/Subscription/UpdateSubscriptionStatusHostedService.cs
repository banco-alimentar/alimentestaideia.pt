// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptionStatusHostedService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices.Subscription
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Http;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Timer = System.Threading.Timer;

    /// <summary>
    /// Update the subscription status of the tenants.
    /// </summary>
    public class UpdateSubscriptionStatusHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<UpdateSubscriptionStatusHostedService> logger;
        private readonly IServiceProvider services;
        private Timer? timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptionStatusHostedService"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="services">Service provider.</param>
        public UpdateSubscriptionStatusHostedService(
            ILogger<UpdateSubscriptionStatusHostedService> logger,
            IServiceProvider services)
        {
            this.logger = logger;
            this.services = services;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.timer = new Timer(
                this.ExecuteSync,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.timer?.Dispose();
        }

        private void ExecuteSync(object? state)
        {
            Task.Run(async () =>
            {
                await this.ExecuteCore(CancellationToken.None);
            });
        }

        private Task ExecuteCore(CancellationToken cancellationToken)
        {
            using (IServiceScope scope = this.services.CreateScope())
            {
                IWebHostEnvironment webHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                ITenantProvider tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                IKeyVaultConfigurationManager keyVaultConfigurationManager = scope.ServiceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
                bool configurationLoaded = keyVaultConfigurationManager.LoadTenantConfiguration();
                if (configurationLoaded)
                {
                    using IInfrastructureUnitOfWork infrastructureUnitOfWork = scope.ServiceProvider.GetRequiredService<IInfrastructureUnitOfWork>();
                    List<Model.Tenant> allTenants = infrastructureUnitOfWork
                        .TenantRepository
                        .GetAllTenantForEnvironment(webHostEnvironment.EnvironmentName);

                    foreach (var tenant in allTenants)
                    {
                        IServiceCollection services = new ServiceCollection();
                        services.AddScoped<IHttpContextAccessor, TenantHttpContextAccessor>();
                        services.AddScoped<TenantHttpNonHttpContext, TenantHttpNonHttpContext>();
                        services.AddScoped<EasyPayBuilder, EasyPayBuilder>();
                        using (IServiceScope tenantScope = services.BuildServiceProvider().CreateScope())
                        {
                            IHttpContextAccessor? httpContextAccessor = tenantScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                            if (httpContextAccessor != null)
                            {
                                httpContextAccessor.HttpContext!.SetTenant(tenant);
                            }
                        }
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
}
