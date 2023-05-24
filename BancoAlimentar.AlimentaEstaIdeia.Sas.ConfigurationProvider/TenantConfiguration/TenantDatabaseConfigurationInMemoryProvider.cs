// -----------------------------------------------------------------------
// <copyright file="TenantDatabaseConfigurationInMemmoryProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This is the read Tenant configuration from the database and keep in memory using a cache system.
    /// </summary>
    public class TenantDatabaseConfigurationInMemoryProvider
    {
        private readonly ApplicationDbContext context;
        private readonly HttpContext httpContext;
        private readonly InMemoryCacheService inMemmoryCacheService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantDatabaseConfigurationInMemoryProvider"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="httpContext"></param>
        /// <param name="inMemoryCacheService"></param>
        public TenantDatabaseConfigurationInMemoryProvider(ApplicationDbContext context,
                                                            HttpContext httpContext,
                                                            InMemoryCacheService inMemoryCacheService)
        {
            this.context = context;
            this.httpContext = httpContext;
            this.inMemmoryCacheService = inMemoryCacheService;
        }

        public Dictionary<string, string> GetTenantConfiguration()
        {
            Dictionary<string, string> result = null;
            Model.Tenant tenant = this.httpContext.GetTenant();
            Dictionary<string, string> tenantConfiguration = this.inMemmoryCacheService.GetTenantConfiguration(tenant.Id);
            if (tenantConfiguration != null && tenantConfiguration.Count > 0)
            {
                result = tenantConfiguration;
            }
            else
            {
                List<Configuration> allConfiguration = this.context.Configurations.ToList();
                result = new Dictionary<string, string>();
                foreach (Configuration configuration in allConfiguration)
                {
                    result.Add(configuration.Name, configuration.Value);
                }

                this.inMemmoryCacheService.SetTenantConfiguration(tenant.Id, result);
            }


            return result;
        }
    }
}
