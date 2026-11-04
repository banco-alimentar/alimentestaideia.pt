// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
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
            if (!configurationLoaded)
            {
                await WriteConfigurationFailureAsync(
                    context,
                    webHostEnvironment,
                    keyVaultConfigurationManager.GetLastLoadDiagnostics());
                return;
            }

            Timing? timing = MiniProfiler.Current.Step("GetTenantData");
            root?.AddChild(timing!);
            TenantData tenantData = tenantProvider.GetTenantData(context);
            Model.Tenant tenant = unitOfWork.TenantRepository.FindTenantByDomainIdentifier(tenantData.Name, webHostEnvironment.EnvironmentName);
            if (tenant == null)
            {
                await context.Response.WriteAsync($"Can't find a valid tenant {tenantData} for environment {webHostEnvironment.EnvironmentName}");
                return;
            }

            context.SetTenant(tenant);
            context.Items[typeof(IKeyVaultConfigurationManager).Name] = keyVaultConfigurationManager;
            timing?.Stop();

            bool shouldInitializeTenant;
            using (timing = MiniProfiler.Current.Step("EnsureTenantConfigurationLoaded"))
            {
                root?.AddChild(timing!);
                shouldInitializeTenant = await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(
                    tenant.Id,
                    tenantData.TenantDevelopmentOptions);
            }

            Dictionary<string, string>? tenantConfiguration = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
            if (tenantConfiguration == null)
            {
                KeyVaultConfigurationLoadDiagnostics loadDiagnostics =
                    keyVaultConfigurationManager.GetLastLoadDiagnostics() ??
                    new KeyVaultConfigurationLoadDiagnostics
                    {
                        Stage = "TenantSecretsNotLoaded",
                        Message = $"Tenant secrets were not loaded for {tenant.Name} (Id {tenant.Id}).",
                        EnvironmentName = webHostEnvironment.EnvironmentName,
                        TenantName = tenant.Name,
                    };

                await WriteConfigurationFailureAsync(context, webHostEnvironment, loadDiagnostics);
                return;
            }

            if (shouldInitializeTenant)
            {
                await context.RequestServices
                    .GetRequiredService<TenantInitializationService>()
                    .InitializeTenant(context, tenant, root, configuration)
                    .ConfigureAwait(false);
            }

            StaticFileConfigurationManager.CreateBlobServiceClient(context, configuration, tenant.NormalizedName, tenant.PublicId);

            if (configuration is TenantConfigurationRoot tenantConfigurationRoot)
            {
                tenantConfigurationRoot.InitTenantDatabaseConfiguration();
            }

            await this.next(context);
            root?.Stop();
        }

        private static Task WriteConfigurationFailureAsync(
            HttpContext context,
            IWebHostEnvironment webHostEnvironment,
            KeyVaultConfigurationLoadDiagnostics? loadDiagnostics)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain; charset=utf-8";
            return context.Response.WriteAsync(
                TenantConfigurationErrorResponse.BuildKeyVaultLoadFailure(webHostEnvironment, loadDiagnostics));
        }
    }
}
