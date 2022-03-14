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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Internal;
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
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<CustomWebApplicationFactory<TStartup>>(optional: true)
                    .AddEnvironmentVariables();
                configuration = config.Build();
            });
            builder.ConfigureServices(services =>
            {
                services.Remove(services.Single(p => p.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)));
                services.Remove(services.Single(d => d.ServiceType == typeof(InfrastructureDbContext)));
                services.Remove(services.Single(p => p.ServiceType == typeof(IKeyVaultConfigurationManager)));
                services.AddTransient<IKeyVaultConfigurationManager, TestKeyVaultConfigurationManager>();
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
                services.AddScoped<InfrastructureDbContext, InfrastructureDbContext>((serviceProvider) =>
                {
                    DbContextOptionsBuilder<InfrastructureDbContext> options = new DbContextOptionsBuilder<InfrastructureDbContext>();
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    InfrastructureDbContext infrastructureDbContext = new InfrastructureDbContext(options.Options);
                    TenantDevelopmentOptions devlopmentOptions = new TenantDevelopmentOptions();
                    configuration.GetSection(TenantDevelopmentOptions.Section).Bind(devlopmentOptions);
                    infrastructureDbContext.Database.EnsureCreated();
                    infrastructureDbContext.Tenants.Add(new Tenant()
                    {
                        Name = devlopmentOptions.Name,
                        Created = DateTime.UtcNow,
                        Domains = new List<DomainIdentifier>()
                        {
                            new DomainIdentifier()
                            {
                                Created = DateTime.UtcNow,
                                DomainName = devlopmentOptions.DomainIdentifier,
                                Environment = "Development",
                            },
                        },
                        InvoicingStrategy = Enum.Parse<InvoicingStrategy>(devlopmentOptions.InvoicingStrategy),
                        PaymentStrategy = Enum.Parse<PaymentStrategy>(devlopmentOptions.PaymentStrategy),
                        PublicId = Guid.NewGuid(),
                    });
                    infrastructureDbContext.Tenants.Add(new Tenant()
                    {
                        Created = DateTime.Now,
                        Domains = new List<DomainIdentifier>()
                        {
                            new DomainIdentifier()
                            {
                                Created = DateTime.UtcNow,
                                DomainName = devlopmentOptions.DomainIdentifier,
                                Environment = "Development",
                            },
                        },
                        Name = "localhost",
                        InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                        PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor,
                        PublicId = Guid.NewGuid(),
                    });
                    infrastructureDbContext.SaveChanges();
                    return infrastructureDbContext;
                });

                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var logger = scopedServices
                        .GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                    context.Database.EnsureCreated();
                    var userManager = sp.GetRequiredService<UserManager<WebUser>>();
                    var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
                    InitDatabase.Seed(context, userManager, roleManager).Wait();
                }
            });
        }
    }
}
