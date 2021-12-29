// -----------------------------------------------------------------------
// <copyright file="CustomWebApplicationFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Testing.Common
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Initializer;
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
                    .AddUserSecrets(Assembly.GetExecutingAssembly())
                    .AddEnvironmentVariables();
                configuration = config.Build();
            });
            builder.ConfigureServices(async services =>
            {
                services.Remove(services.Single(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<ApplicationDbContext>)));

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForIntegrationTesting");
                });

                services.Remove(services.Single(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<InfrastructureDbContext>)));

                services.AddDbContext<InfrastructureDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForIntegrationTesting");
                });

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();
                var infrastructureContext = scopedServices.GetRequiredService<InfrastructureDbContext>();
                infrastructureContext.Database.EnsureCreated();

                try
                {
                    var userManager = scopedServices.GetRequiredService<UserManager<WebUser>>();
                    var roleManager = scopedServices.GetRequiredService<RoleManager<ApplicationRole>>();
                    await InitDatabase.Seed(context, userManager, roleManager);
                    TenantDbInitializer.Initialize(infrastructureContext);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred seeding the database with test messages. Error: {ex.Message}");
                }
            });
        }
    }
}
