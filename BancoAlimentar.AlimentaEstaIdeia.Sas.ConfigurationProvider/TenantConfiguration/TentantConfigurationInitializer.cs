// -----------------------------------------------------------------------
// <copyright file="TentantConfigurationInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Tenant configuration initializer.
/// </summary>
public abstract class TentantConfigurationInitializer
{
    private static List<Type> tenantConfigurationTypes;

    static TentantConfigurationInitializer()
    {
        tenantConfigurationTypes = Assembly.GetExecutingAssembly()
           .GetTypes()
           .Where(p => !p.IsAbstract && p.IsAssignableTo(typeof(TentantConfigurationInitializer)))
           .ToList();
    }

    /// <summary>
    /// Explore all initializers in code and call them.
    /// </summary>
    /// <param name="config">Tenant specific configuration.</param>
    /// <param name="services">List of services to configure for the tenant.</param>
    public static void InitializeTenant(Dictionary<string, string> config, IServiceCollection services)
    {
        foreach (var item in tenantConfigurationTypes)
        {
            TentantConfigurationInitializer? target = Activator.CreateInstance(item) as TentantConfigurationInitializer;
            if (target != null)
            {
                target.InitializeTenantConfiguration(config, services);
            }
        }
    }

    /// <summary>
    /// Migrate database to the latest version.
    /// </summary>
    /// <param name="context">A reference to the <see cref="ApplicationDbContext"/>.</param>
    /// <param name="token">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task MigrateDatabaseAsync(ApplicationDbContext context, CancellationToken token)
    {
        if (context.Database.IsRelational() && (await context.Database.GetPendingMigrationsAsync(token)).Any())
        {
            await context.Database.MigrateAsync(token);
        }
    }

    /// <summary>
    /// Initialize the specific configuration for the tenant.
    /// </summary>
    /// <param name="config">Tenant specific configuration.</param>
    /// <param name="services">List of services to configure for the tenant.</param>
    public abstract void InitializeTenantConfiguration(Dictionary<string, string> config, IServiceCollection services);
}
