// -----------------------------------------------------------------------
// <copyright file="CustomWebApplicationFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
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
                configuration = config.Build();
            });
            builder.ConfigureServices(async services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<ApplicationDbContext>));

                services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    // options.UseSqlServer(configuration.GetConnectionString("IntegrationTestConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
                    options.UseInMemoryDatabase("InMemoryDbForIntegrationTesting");
                });

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger = scopedServices
                    .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                context.Database.EnsureCreated();

                try
                {
                    var userManager = scopedServices.GetRequiredService<UserManager<WebUser>>();
                    var roleManager = scopedServices.GetRequiredService<RoleManager<ApplicationRole>>();
                    await InitDatabase.Seed(context, userManager, roleManager);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred seeding the database with test messages. Error: {ex.Message}");
                }
            });
        }
    }
}
