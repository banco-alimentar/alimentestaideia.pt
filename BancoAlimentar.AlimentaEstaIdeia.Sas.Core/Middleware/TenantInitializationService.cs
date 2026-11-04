// -----------------------------------------------------------------------
// <copyright file="TenantInitializationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using StackExchange.Profiling;

    /// <summary>
    /// Initialize the tenant database configuration once per process.
    /// </summary>
    public class TenantInitializationService
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> tenantInitializationLocks = new ConcurrentDictionary<int, SemaphoreSlim>();
        private readonly ConcurrentDictionary<int, bool> tenantInitializationStatus = new ConcurrentDictionary<int, bool>();

        /// <summary>
        /// Initializes the tenant database configuration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task InitializeTenant(HttpContext context, Tenant tenant, Timing? root, IConfiguration configuration)
        {
            if (this.IsTenantInitialized(tenant.Id))
            {
                return;
            }

            SemaphoreSlim initializationLock = this.tenantInitializationLocks.GetOrAdd(tenant.Id, _ => new SemaphoreSlim(1, 1));
            await initializationLock.WaitAsync(CancellationToken.None);
            try
            {
                if (this.IsTenantInitialized(tenant.Id))
                {
                    return;
                }

                using (Timing? timing = MiniProfiler.Current.Step("SeedAndMigrationsTenantDatabase"))
                {
                    root?.AddChild(timing!);
                    IServiceProvider currentServiceProvider = context.RequestServices;
                    ApplicationDbContext applicationDbContext = currentServiceProvider.GetRequiredService<ApplicationDbContext>();

                    IConfiguration hostConfiguration = configuration is TenantConfigurationRoot tenantConfiguration
                        ? tenantConfiguration.HostConfiguration
                        : configuration;
                    if (hostConfiguration.GetSection("LocalEmailCapture").GetValue<bool>("Enabled"))
                    {
                        if (applicationDbContext.Database.IsRelational() && applicationDbContext.Database.GetPendingMigrations().Any())
                        {
                            throw new InvalidOperationException("Local email tests require an initialized schema. Apply migrations explicitly before starting the local browser tests.");
                        }
                    }
                    else
                    {
                        // Migrations are server-side work; do not cancel when the HTTP client disconnects.
                        await TentantConfigurationInitializer.MigrateDatabaseAsync(
                            applicationDbContext,
                            currentServiceProvider.GetRequiredService<TelemetryClient>(),
                            tenant,
                            CancellationToken.None);
                    }

                    await InitDatabase.Seed(
                        applicationDbContext,
                        currentServiceProvider.GetRequiredService<UserManager<WebUser>>(),
                        currentServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
                        configuration);

                    this.tenantInitializationStatus[tenant.Id] = true;
                }
            }
            finally
            {
                initializationLock.Release();
            }
        }

        private bool IsTenantInitialized(int tenantId)
        {
            return this.tenantInitializationStatus.TryGetValue(tenantId, out bool initialized) && initialized;
        }
    }
}
