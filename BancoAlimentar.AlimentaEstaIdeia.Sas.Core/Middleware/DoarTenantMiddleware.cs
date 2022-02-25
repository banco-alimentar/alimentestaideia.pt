// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using StackExchange.Profiling;

    /// <summary>
    /// Doar+ tenant midleware default implementation.
    /// </summary>
    public class DoarTenantMiddleware
    {
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
        /// <param name="services">Services.</param>
        /// <returns>A task to monitor progress.</returns>
        public async Task Invoke(
            HttpContext context,
            ITenantProvider tenantProvider,
            IInfrastructureUnitOfWork unitOfWork,
            IKeyVaultConfigurationManager keyVaultConfigurationManager,
            IServiceCollection services)
        {
            Timing? root = MiniProfiler.Current.Step("MultitenantMiddleware");
            TenantData tenantData = new TenantData(string.Empty);
            Model.Tenant tenant = new Model.Tenant();
            using (var timing = MiniProfiler.Current.Step("GetTenantData"))
            {
                root?.AddChild(timing);
                tenantData = tenantProvider.GetTenantData(context);
                tenant = unitOfWork.TenantRepository.FindTenantByDomainIdentifier(tenantData.Name);
                context.SetTenant(tenant);
                context.Items[typeof(IKeyVaultConfigurationManager).Name] = keyVaultConfigurationManager;
            }

            bool tenantConfigurationLoaded = false;
            using (var timing = MiniProfiler.Current.Step("EnsureTenantConfigurationLoaded"))
            {
                root?.AddChild(timing);
                tenantConfigurationLoaded = await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id);
            }

            Dictionary<string, string>? tenantConfiguration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
            if (tenantConfiguration != null)
            {
                ServiceCollection newServices = new ServiceCollection();
                int index = 0;
                foreach (var item in services)
                {
                    newServices.Insert(index, item);
                    index++;
                }

                using (var timing = MiniProfiler.Current.Step("InitializeTenant"))
                {
                    root?.AddChild(timing);
                    TentantConfigurationInitializer.InitializeTenant(tenantConfiguration, newServices);
                }

                context.RequestServices = newServices.BuildServiceProvider();
                if (tenantConfigurationLoaded)
                {
                    using (var timing = MiniProfiler.Current.Step("SeedAndMigrationsTenantDatabase"))
                    {
                        root?.AddChild(timing);
                        ApplicationDbContext applicationDbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                        await TentantConfigurationInitializer.MigrateDatabaseAsync(applicationDbContext, context.RequestAborted);
                        await InitDatabase.Seed(
                            applicationDbContext,
                            context.RequestServices.GetRequiredService<UserManager<WebUser>>(),
                            context.RequestServices.GetRequiredService<RoleManager<ApplicationRole>>());
                    }
                }

                root?.Stop();
                await this.next(context);
            }
            else
            {
                await context.Response.WriteAsync($"TenantConfiguration is null for {tenant.Name} Id {tenant.Id}");
            }
        }
    }
}
