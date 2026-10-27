// -----------------------------------------------------------------------
// <copyright file="PersistenceServiceCollectionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Registers application persistence services.
    /// </summary>
    public static class PersistenceServiceCollectionExtensions
    {
        /// <summary>
        /// Adds database contexts, units of work, and repositories.
        /// </summary>
        /// <param name="services">The collection that receives persistence registrations.</param>
        /// <param name="configuration">The host configuration.</param>
        /// <param name="environment">The host environment.</param>
        /// <returns>The configured persistence service collection.</returns>
        public static IServiceCollection AddApplicationPersistence(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddScoped<IInfrastructureUnitOfWork, InfrastructureUnitOfWork>();
            services.AddScoped<ProductCatalogueRepository>();
            services.AddScoped<FoodBankRepository>();
            services.AddScoped<DonationItemRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<DonationRepository>();

            services.AddDbContextFactory<ApplicationDbContext>(
                (serviceProvider, options) =>
                {
                    IConfiguration tenantConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                    LocalEmailCapture.ValidateDataAccess(tenantConfiguration, environment.EnvironmentName);
                    string connectionString = tenantConfiguration["ConnectionStrings:DefaultConnection"];
                    options.UseSqlServer(connectionString, builder =>
                    {
                        builder.EnableRetryOnFailure();
                        builder.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web");
                        builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                },
                lifetime: ServiceLifetime.Scoped);

            // Local capture ignores inherited infrastructure SQL connections.
            if (!environment.IsDevelopment() ||
                (!LocalEmailCapture.UsesInMemoryInfrastructure(configuration) &&
                 !string.IsNullOrEmpty(configuration["ConnectionStrings:Infrastructure"])))
            {
                services.AddDbContext<InfrastructureDbContext>(options =>
                    options.UseSqlServer(
                        configuration.GetConnectionString("Infrastructure"),
                        builder =>
                        {
                            builder.EnableRetryOnFailure();
                            builder.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Sas.Model");
                            builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        }));
                return services;
            }

            services.AddScoped<InfrastructureDbContext>(serviceProvider =>
            {
                DbContextOptionsBuilder<InfrastructureDbContext> options =
                    new DbContextOptionsBuilder<InfrastructureDbContext>();
                SqliteConnection connection = new SqliteConnection("Filename=:memory:");
                connection.Open();
                options.UseSqlite(connection);
                InfrastructureDbContext infrastructureDbContext = new InfrastructureDbContext(options.Options);
                TenantDevelopmentOptions developmentOptions = new TenantDevelopmentOptions();
                configuration.GetSection(TenantDevelopmentOptions.Section).Bind(developmentOptions);
                infrastructureDbContext.Database.EnsureCreated();
                infrastructureDbContext.Tenants.Add(new Tenant()
                {
                    Name = developmentOptions.Name,
                    Created = DateTime.UtcNow,
                    Domains = new List<DomainIdentifier>()
                    {
                        new DomainIdentifier()
                        {
                            Created = DateTime.UtcNow,
                            DomainName = developmentOptions.DomainIdentifier,
                            Environment = "localhost",
                        },
                    },
                    InvoicingStrategy = Enum.Parse<InvoicingStrategy>(developmentOptions.InvoicingStrategy),
                    PaymentStrategy = Enum.Parse<PaymentStrategy>(developmentOptions.PaymentStrategy),
                    PublicId = Guid.NewGuid(),
                });
                infrastructureDbContext.SaveChanges();
                return infrastructureDbContext;
            });
            return services;
        }
    }
}
