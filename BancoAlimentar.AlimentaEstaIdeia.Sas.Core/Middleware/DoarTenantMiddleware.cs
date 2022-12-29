// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using StackExchange.Profiling;

    /// <summary>
    /// Doar+ tenant midleware default implementation.
    /// </summary>
    public class DoarTenantMiddleware
    {
        private static object sharedLock = new object();
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoarTenantMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate.</param>
        public DoarTenantMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context">Current http context.</param>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="unitOfWork">Infrastructure unit of work.</param>
        /// <param name="keyVaultConfigurationManager">Azure Key Vault Configuration Manager.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>A task to monitor progress.</returns>
        public async Task Invoke(
            HttpContext context,
            ITenantProvider tenantProvider,
            IInfrastructureUnitOfWork unitOfWork,
            IKeyVaultConfigurationManager keyVaultConfigurationManager,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            Timing? root = MiniProfiler.Current.Step("MultitenantMiddleware");
            keyVaultConfigurationManager.LoadTenantConfiguration();
            Timing timing = MiniProfiler.Current.Step("GetTenantData");
            root?.AddChild(timing);
            TenantData tenantData = tenantProvider.GetTenantData(context);
            Model.Tenant tenant = unitOfWork.TenantRepository.FindTenantByDomainIdentifier(tenantData.Name, webHostEnvironment.EnvironmentName);
            if (tenant != null)
            {
                context.SetTenant(tenant);
                context.Items[typeof(IKeyVaultConfigurationManager).Name] = keyVaultConfigurationManager;
                timing?.Stop();

                bool tenantConfigurationLoaded = false;
                using (timing = MiniProfiler.Current.Step("EnsureTenantConfigurationLoaded"))
                {
                    root?.AddChild(timing);
                    tenantConfigurationLoaded = await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(
                        tenant.Id,
                        tenantData.TenantDevelopmentOptions);
                }

                Dictionary<string, string>? tenantConfiguration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
                if (tenantConfiguration != null)
                {
                    if (tenantConfigurationLoaded)
                    {
                        bool isLockTaken = false;
                        Monitor.Enter(sharedLock, ref isLockTaken);
                        if (isLockTaken)
                        {
                            using (timing = MiniProfiler.Current.Step("SeedAndMigrationsTenantDatabase"))
                            {
                                root?.AddChild(timing);
                                IServiceProvider currentServiceProvider = context.RequestServices;
                                ApplicationDbContext applicationDbContext = currentServiceProvider.GetRequiredService<ApplicationDbContext>();
                                await TentantConfigurationInitializer.MigrateDatabaseAsync(applicationDbContext, context.RequestAborted);
                                await InitDatabase.Seed(
                                    applicationDbContext,
                                    currentServiceProvider.GetRequiredService<UserManager<WebUser>>(),
                                    currentServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
                                    configuration);
                            }

                            if (Monitor.IsEntered(sharedLock))
                            {
                                Monitor.Exit(sharedLock);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Could not acquire lock for tenant configuration initialization.");
                        }
                    }

                    StaticFileConfigurationManager.CreateBlobServiceClient(context, configuration, tenant.NormalizedName, tenant.PublicId);

                    await this.next(context);
                    root?.Stop();
                }
                else
                {
                    await context.Response.WriteAsync($"TenantConfiguration is null for {tenant.Name} Id {tenant.Id} Env:{webHostEnvironment.EnvironmentName}");
                }
            }
            else
            {
                await context.Response.WriteAsync($"Can't find a valid tenant {tenantData} for environment {webHostEnvironment.EnvironmentName}");
            }
        }
    }
}
