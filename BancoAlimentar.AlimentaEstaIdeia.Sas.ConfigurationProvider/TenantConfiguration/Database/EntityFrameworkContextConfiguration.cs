// -----------------------------------------------------------------------
// <copyright file="EntityFrameworkContextConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Database;

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configure the Entity Framework Context for the tenant.
/// </summary>
public class EntityFrameworkContextConfiguration : TentantConfigurationInitializer
{
    /// <inheritdoc/>
    public override void InitializeTenantConfiguration(Dictionary<string, string> config, IServiceCollection services)
    {
        // services.AddDbContextFactory<ApplicationDbContext>(options =>
        // {
        //    options.UseSqlServer(
        //          config["ConnectionStrings:DefaultConnection"], b =>
        //          {
        //              b.EnableRetryOnFailure();
        //              b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web");
        //              b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        //          });
        // });

        // services.AddDbContext<ApplicationDbContext>(options =>
        //    options.UseSqlServer(
        //        config["ConnectionStrings:DefaultConnection"], b =>
        //        {
        //            b.EnableRetryOnFailure();
        //            b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web");
        //            b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        //        }));
    }
}
