// -----------------------------------------------------------------------
// <copyright file="CustomWebApplicationFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Testing.Common
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// <see cref="CustomWebApplicationFactory{TStartup}"/> test class.
    /// </summary>
    /// <typeparam name="TStartup">Startup class.</typeparam>
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup>
        where TStartup : class
    {
        /// <summary>
        /// Confures the ASP.NET Core host for the Integration Testing.
        /// </summary>
        /// <param name="builder">A reference to the <see cref="IWebHostBuilder"/>.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            IConfiguration configuration = null;
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config
                    .AddUserSecrets<CustomWebApplicationFactory<TStartup>>(optional: true)
                    .AddEnvironmentVariables();
                configuration = config.Build();
            });
            builder.ConfigureServices(async services =>
            {
                services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)));
                services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<InfrastructureDbContext>)));
                services.Remove(services.Single(p => p.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>)));
                services.Remove(services.Single(p => p.ServiceType == typeof(IKeyVaultConfigurationManager)));
                services.AddTransient<IKeyVaultConfigurationManager, TestKeyVaultConfigurationManager>();
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    // options.UseSqlServer(configuration.GetConnectionString("IntegrationTestConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
                    options.UseInMemoryDatabase("InMemoryDbForIntegrationTesting");
                });

                services.AddDbContext<InfrastructureDbContext>(options =>
                {
                    // options.UseSqlServer(configuration.GetConnectionString("IntegrationTestConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
                    options.UseInMemoryDatabase("InMemoryDbForIntegrationTesting");
                });

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var infrastructureContext = scopedServices.GetRequiredService<InfrastructureDbContext>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                context.Database.EnsureCreated();
                infrastructureContext.Database.EnsureCreated();

                try
                {
                    var userManager = scopedServices.GetRequiredService<UserManager<WebUser>>();
                    var roleManager = scopedServices.GetRequiredService<RoleManager<ApplicationRole>>();
                    await InitDatabase.Seed(context, userManager, roleManager);
                    infrastructureContext.Tenants.Add(new Tenant()
                    {
                        Created = DateTime.Now,
                        DomainIdentifier = "localhost",
                        Id = 1,
                        Name = "localhost",
                        InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                        PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor,
                        PublicId = Guid.NewGuid(),
                    });
                    infrastructureContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred seeding the database with test messages. Error: {ex.Message}");
                }
            });
        }
    }
}
