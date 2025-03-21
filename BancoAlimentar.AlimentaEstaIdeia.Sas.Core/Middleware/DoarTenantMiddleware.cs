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
    using Microsoft.ApplicationInsights;
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
            bool configurationLoaded = keyVaultConfigurationManager.LoadTenantConfiguration();
            if (configurationLoaded)
            {
                Timing? timing = MiniProfiler.Current.Step("GetTenantData");
                root?.AddChild(timing!);
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
                        root?.AddChild(timing!);
                        tenantConfigurationLoaded = await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(
                            tenant.Id,
                            tenantData.TenantDevelopmentOptions);
                    }

                    Dictionary<string, string>? tenantConfiguration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
                    if (tenantConfiguration != null)
                    {
                        if (tenantConfigurationLoaded)
                        {
                            await context.RequestServices
                                .GetRequiredService<TentantInitializationService>()
                                .InitializeTenant(context, tenant, root!, configuration)
                                .ConfigureAwait(false);
                        }

                        StaticFileConfigurationManager.CreateBlobServiceClient(context, configuration, tenant.NormalizedName, tenant.PublicId);

                        if (configuration is TenantConfigurationRoot)
                        {
                            TenantConfigurationRoot tenantConfigurationRoot = (TenantConfigurationRoot)configuration;
                            tenantConfigurationRoot.InitTenantDatabaseConfiguration();
                        }

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
            else
            {
                await context.Response.WriteAsync($"There was an error loading the tenant configuration from the KeyVault please take a look at Application Insights.");
            }
        }
    }
}
