// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Service collection extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register tenant specific options.
        /// </summary>
        /// <typeparam name="TOptions">Type of options we are apply configuration to.</typeparam>
        public static void WithPerTenantOptions<TOptions>(this IServiceCollection services)
            where TOptions : class, new()
        {
            // Register the multi-tenant cache
            services.AddSingleton<IOptionsMonitorCache<TOptions>, TenantOptionsCache<TOptions>>();

            // Register the multi-tenant options factory
            services.AddTransient<IOptionsFactory<TOptions>, TenantOptionsFactory<TOptions>>();

            // Register IOptionsSnapshot support
            services.AddScoped<IOptionsSnapshot<TOptions>, TenantOptions<TOptions>>();

            // Register IOptions support
            services.AddSingleton<IOptions<TOptions>, TenantOptions<TOptions>>();
        }
    }
}
