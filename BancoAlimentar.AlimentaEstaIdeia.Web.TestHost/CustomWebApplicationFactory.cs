// -----------------------------------------------------------------------
// <copyright file="CustomWebApplicationFactory.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Hosts the web application under test for integration tests.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Avoid loading optional package hosting-startup assemblies that are not copied to every test output folder.
            builder.UseSetting(WebHostDefaults.PreventHostingStartupKey, bool.TrueString);
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        // Web appsettings.Development.json is copied into the test output and contains
                        // deployment token placeholders; skip Azure blob seeding and use embedded food banks.
                        ["AzureStorage:FoodBankSourceBlobName"] = string.Empty,
                        ["AzureStorage:FoodBankSourceContainerName"] = string.Empty,

                        // Parseable dev storage connection for tenant static files when secrets are absent (CI).
                        ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                        ["AzureStorage:SinglePaymentAuditingTableName"] = "IntegrationTestSinglePaymentAuditing",
                        ["AzureStorage:PdfContainerName"] = "integration-test-pdfs",
                        ["IntegrationTesting:SkipAzureTableAuditing"] = bool.TrueString,
                    })
                    .AddUserSecrets<CustomWebApplicationFactory>(optional: true);
            });
            builder.ConfigureServices((context, services) =>
            {
                this.ConfigureTestDatabaseServices(services, context.Configuration);
                this.ConfigureTestHostServices(services, context.Configuration);
                this.RemoveTenantStaticSyncHostedService(services);
                this.ConfigureEnglishLocalizationForTests(services);
                services.Replace(ServiceDescriptor.Scoped<IEasyPayWebhookVerifier, IntegrationTestEasyPayWebhookVerifier>());
            });
        }

        /// <inheritdoc />
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = false;
                options.ValidateOnBuild = false;
            });
            return base.CreateHost(builder);
        }

        private static void RemoveEfCoreProviderConfiguration<TContext>(IServiceCollection services)
            where TContext : DbContext
        {
            foreach (var descriptor in services
                .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>))
                .ToList())
            {
                services.Remove(descriptor);
            }
        }

        private void RemoveTenantStaticSyncHostedService(IServiceCollection services)
        {
            foreach (ServiceDescriptor descriptor in services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(TenantStaticSyncHostedService))
                .ToList())
            {
                services.Remove(descriptor);
            }
        }

        private void ConfigureEnglishLocalizationForTests(IServiceCollection services)
        {
            services.PostConfigure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en", "en");
            });
        }

        private void ConfigureTestHostServices(IServiceCollection services, IConfiguration configuration)
        {
            services.RemoveAll(typeof(IConfiguration));
            services.AddSingleton<IConfiguration>(sp =>
                new TenantConfigurationRoot(
                    configuration,
                    sp.GetRequiredService<IHttpContextAccessor>()));

            var serviceProviderOptions = new ServiceProviderOptions
            {
                ValidateScopes = false,
                ValidateOnBuild = false,
            };
            var sp = services.BuildServiceProvider(serviceProviderOptions);

            using (var scope = sp.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                context.Database.EnsureCreated();
                InitDatabase.Seed(context, userManager, roleManager, configuration).Wait();
            }
        }

        private void ConfigureTestDatabaseServices(IServiceCollection services, IConfiguration configuration)
        {
            RemoveEfCoreProviderConfiguration<ApplicationDbContext>(services);
            RemoveEfCoreProviderConfiguration<InfrastructureDbContext>(services);
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(InfrastructureDbContext));
            services.RemoveAll(typeof(IKeyVaultConfigurationManager));
            services.AddTransient<IKeyVaultConfigurationManager, TestKeyVaultConfigurationManager>();
            string applicationDatabaseName = Guid.NewGuid().ToString();
            services.AddScoped<ApplicationDbContext, ApplicationDbContext>((serviceProvider) =>
            {
                DbContextOptionsBuilder<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>();
                options.UseInMemoryDatabase(applicationDatabaseName)
                    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                ApplicationDbContext applicationDbContext = new ApplicationDbContext(options.Options);
                applicationDbContext.Database.EnsureCreated();
                return applicationDbContext;
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
                infrastructureDbContext.SaveChanges();
                return infrastructureDbContext;
            });
        }
    }
}
