using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.Extensions.Configuration;
using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
using Microsoft.AspNetCore.Identity;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup: class
    {
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
                    //options.UseSqlServer(configuration.GetConnectionString("IntegrationTestConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
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
                    ProductCatalogueDbInitializer.Initialize(context);
                    AnonymousUserDbInitializer.Initialize(context);
                    FoodBankDbInitializer.Initialize(context);
                    var userManager = scopedServices.GetRequiredService<UserManager<WebUser>>();
                    var roleManager = scopedServices.GetRequiredService<RoleManager<ApplicationRole>>();

                    await RolesDbInitializer.SeedRolesAsync(userManager, roleManager);                    
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the " +
                        "database with test messages. Error: {Message}", ex.Message);
                }
            });
        }
    }
}
