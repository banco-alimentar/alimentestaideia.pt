﻿// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System.Diagnostics;
    using System.Net.Security;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

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
            KeyVaultConfigurationManager keyVaultConfigurationManager,
            IServiceCollection services)
        {
            TenantData tenantData = tenantProvider.GetTenantData(context);
            Model.Tenant tenant = unitOfWork.TenantRepository.FindTenantByDomainIdentifier(tenantData.Name);

            context.SetTenant(tenant);
            context.Items[typeof(KeyVaultConfigurationManager).Name] = keyVaultConfigurationManager;
            bool tenantConfigurationLoaded = await keyVaultConfigurationManager.EnsureTenantConfigurationLoaded(tenant.Id);
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

                TentantConfigurationInitializer.InitializeTenant(tenantConfiguration, newServices);
                context.RequestServices = newServices.BuildServiceProvider();
                if (tenantConfigurationLoaded)
                {
                    await InitDatabase.Seed(
                        context.RequestServices.GetService<ApplicationDbContext>(),
                        context.RequestServices.GetService<UserManager<WebUser>>(),
                        context.RequestServices.GetService<RoleManager<ApplicationRole>>());
                }

                await this.next(context);
            }
            else
            {
                await context.Response.WriteAsync($"TenantConfiguration is null for {tenant.Name} Id {tenant.Id}");
            }
        }
    }
}
