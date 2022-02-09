// -----------------------------------------------------------------------
// <copyright file="TentantConfigurationInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Tenant configuration initializer.
    /// </summary>
    public abstract class TentantConfigurationInitializer
    {
        /// <summary>
        /// Explore all initializers in code and call them.
        /// </summary>
        /// <param name="config">Tenant specific configuration.</param>
        /// <param name="services">List of services to configure for the tenant.</param>
        public static void InitializeTenant(Dictionary<string, string> config, IServiceCollection services)
        {
            List<Type> intializers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(p => !p.IsAbstract && p.IsAssignableTo(typeof(TentantConfigurationInitializer)))
                .ToList();
            foreach (var item in intializers)
            {
                TentantConfigurationInitializer? target = Activator.CreateInstance(item) as TentantConfigurationInitializer;
                if (target != null)
                {
                    target.InitializeTenantConfiguration(config, services);
                }
            }
        }

        /// <summary>
        /// Initialize the specific configuration for the tenant.
        /// </summary>
        /// <param name="config">Tenant specific configuration.</param>
        /// <param name="services">List of services to configure for the tenant.</param>
        public abstract void InitializeTenantConfiguration(Dictionary<string, string> config, IServiceCollection services);
    }
}
