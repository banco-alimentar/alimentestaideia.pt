// -----------------------------------------------------------------------
// <copyright file="TenancyServiceCollectionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Matching;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Registers tenant-aware application services.
    /// </summary>
    public static class TenancyServiceCollectionExtensions
    {
        /// <summary>
        /// Adds tenant resolution, configuration, routing, and static-file services.
        /// </summary>
        /// <param name="services">The collection that receives tenant registrations.</param>
        /// <param name="rootConfiguration">The host configuration.</param>
        /// <returns>The configured tenant service collection.</returns>
        public static IServiceCollection AddWebTenancy(
            this IServiceCollection services,
            IConfiguration rootConfiguration)
        {
            services.DecorateEndpointSelector();
            services.AddSingleton<TenantInitializationService>();
            services.AddTransient<IKeyVaultConfigurationManager, KeyVaultConfigurationManager>();
            services.AddSingleton<INamingStrategy, DomainNamingStrategy>();
            services.AddSingleton<INamingStrategy, PathNamingStrategy>();
            services.AddSingleton<INamingStrategy, SubdomainNamingStrategy>();
            services.AddSingleton<ITenantProvider, TenantProvider>();
            services.AddTransient<ITenantLayout, TenantLayout>();
            services.AddSingleton<InMemoryCacheService>();
            services.AddTransient<TenantDatabaseConfigurationInMemoryProvider>();
            services.AddSingleton<LocalDevelopmentOverride>();
            services.AddScoped<IStaticFileMapper, StaticFileMapper>();

            ServiceDescriptor configurationDescriptor = services
                .LastOrDefault(descriptor => descriptor.ServiceType == typeof(IConfiguration))
                ?? throw new InvalidOperationException("The host IConfiguration service is not registered.");
            services.Remove(configurationDescriptor);
            services.AddScoped<IConfiguration>(serviceProvider =>
                new TenantConfigurationRoot(
                    rootConfiguration,
                    serviceProvider.GetRequiredService<IHttpContextAccessor>()));

            return services;
        }

        /// <summary>
        /// Adds tenant static-file cache synchronization after the remaining application services.
        /// </summary>
        /// <param name="services">The collection that receives tenant static-file registrations.</param>
        /// <returns>The configured tenant static-file service collection.</returns>
        public static IServiceCollection AddTenantStaticFileSynchronization(this IServiceCollection services)
        {
            services.AddSingleton<ITenantStaticLocalCacheService, TenantStaticLocalCacheService>();
            services.AddHostedService<TenantStaticSyncHostedService>();
            return services;
        }

        private static void DecorateEndpointSelector(this IServiceCollection services)
        {
            ServiceDescriptor selectorDescriptor = services
                .LastOrDefault(descriptor => descriptor.ServiceType == typeof(EndpointSelector))
                ?? throw new InvalidOperationException("The default EndpointSelector service is not registered.");

            services.Remove(selectorDescriptor);
            services.Add(new ServiceDescriptor(
                typeof(EndpointSelector),
                serviceProvider => new DoarTenantEndpointSelector(
                    CreateEndpointSelector(serviceProvider, selectorDescriptor),
                    serviceProvider.GetRequiredService<EndpointDataSource>()),
                selectorDescriptor.Lifetime));
        }

        private static EndpointSelector CreateEndpointSelector(
            IServiceProvider serviceProvider,
            ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance is EndpointSelector instance)
            {
                return instance;
            }

            if (descriptor.ImplementationFactory != null)
            {
                return (EndpointSelector)descriptor.ImplementationFactory(serviceProvider);
            }

            if (descriptor.ImplementationType != null)
            {
                return (EndpointSelector)ActivatorUtilities.CreateInstance(
                    serviceProvider,
                    descriptor.ImplementationType);
            }

            throw new InvalidOperationException("The default EndpointSelector registration cannot be activated.");
        }
    }
}
