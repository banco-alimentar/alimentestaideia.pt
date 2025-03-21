// -----------------------------------------------------------------------
// <copyright file="TentantInitializationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Polly;
    using StackExchange.Profiling;

    /// <summary>
    /// Initialize the tenant database configuration once per process.
    /// </summary>
    public class TentantInitializationService
    {
        private static readonly object SharedLock = new object();
        private Dictionary<int, bool> tenantInitializationStatus = new Dictionary<int, bool>();

        /// <summary>
        /// Initializes the tenant database configuration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task InitializeTenant(HttpContext context, Tenant tenant, Timing root, IConfiguration configuration)
        {
            if (!this.tenantInitializationStatus.ContainsKey(tenant.Id) ||
                (this.tenantInitializationStatus.ContainsKey(tenant.Id) && this.tenantInitializationStatus[tenant.Id] == false))
            {
                bool isLockTaken = false;
                try
                {
                    Monitor.Enter(SharedLock, ref isLockTaken);
                    if (isLockTaken)
                    {
                        using (Timing? timing = MiniProfiler.Current.Step("SeedAndMigrationsTenantDatabase"))
                        {
                            root.AddChild(timing!);
                            IServiceProvider currentServiceProvider = context.RequestServices;
                            ApplicationDbContext applicationDbContext = currentServiceProvider.GetRequiredService<ApplicationDbContext>();
                            await TentantConfigurationInitializer.MigrateDatabaseAsync(
                                applicationDbContext,
                                currentServiceProvider.GetRequiredService<TelemetryClient>(),
                                tenant,
                                context.RequestAborted);
                            await InitDatabase.Seed(
                                applicationDbContext,
                                currentServiceProvider.GetRequiredService<UserManager<WebUser>>(),
                                currentServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
                                configuration);

                            this.tenantInitializationStatus.Add(tenant.Id, true);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Could not acquire lock for tenant configuration initialization.");
                    }
                }
                finally
                {
                    if (isLockTaken)
                    {
                        if (Monitor.IsEntered(SharedLock))
                        {
                            Monitor.Exit(SharedLock);
                        }
                    }
                }
            }
        }
    }
}
