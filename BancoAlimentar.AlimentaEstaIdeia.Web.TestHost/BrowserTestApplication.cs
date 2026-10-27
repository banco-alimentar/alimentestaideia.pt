// -----------------------------------------------------------------------
// <copyright file="BrowserTestApplication.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Initializer;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using PayPalCheckoutSdk.Core;

    /// <summary>
    /// Owns a real loopback Development website and relational memory databases for browser tests.
    /// </summary>
    public sealed class BrowserTestApplication : IAsyncDisposable
    {
        private readonly SqliteConnection applicationAnchor;
        private readonly SqliteConnection infrastructureAnchor;
        private readonly BrowserFactory factory;
        private readonly BrowserTestUploads uploads;
        private bool disposed;

        private BrowserTestApplication(int smtpPort)
        {
            string identity = Guid.NewGuid().ToString("N");
            this.applicationAnchor = new SqliteConnection($"Data Source=browser_{identity};Mode=Memory;Cache=Shared;Foreign Keys=True");
            this.infrastructureAnchor = new SqliteConnection($"Data Source=infra_{identity};Mode=Memory;Cache=Shared;Foreign Keys=True");
            var values = CreateSettings(identity, smtpPort);
            this.uploads = new BrowserTestUploads();
            this.factory = new BrowserFactory(values, this.applicationAnchor.ConnectionString, this.infrastructureAnchor.ConnectionString, this.uploads);
        }

        /// <summary>Gets the ephemeral local origin reachable by Chrome.</summary>
        public Uri BaseAddress { get; private set; }

        /// <summary>Gets application services for scoped verification of this host's fixtures.</summary>
        public IServiceProvider Services => this.factory.Services;

        /// <summary>Gets safe configuration including generated local credentials and fixture identifiers.</summary>
        public IConfiguration Configuration { get; private set; }

        /// <summary>Gets this host's owned temporary image-upload directory.</summary>
        public string UploadRoot => this.uploads.Root;

        /// <summary>Gets the loopback payment provider, which cannot mutate donation records.</summary>
        public BrowserTestPayments Payments => this.Services.GetRequiredService<BrowserTestPayments>();

        /// <summary>Gets invalid donation field names, without values, for local failure diagnostics.</summary>
        public string LastDonationErrors => this.Services.GetRequiredService<BrowserTestDonationDiagnostics>().Fields;

        /// <summary>Starts the isolated Development host without SQL Server or cloud settings.</summary>
        /// <param name="smtpPort">Loopback Mailpit SMTP port.</param>
        /// <returns>The started test application, which must be disposed after use.</returns>
        public static async Task<BrowserTestApplication> StartAsync(int smtpPort = 1025)
        {
            if (smtpPort < 1 || smtpPort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(smtpPort));
            }

            var application = new BrowserTestApplication(smtpPort);
            try
            {
                await application.applicationAnchor.OpenAsync();
                await application.infrastructureAnchor.OpenAsync();
                application.factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
                application.factory.StartServer();
                var server = application.Services.GetRequiredService<IServer>();
                string address = server.Features.Get<IServerAddressesFeature>().Addresses.Single();
                application.BaseAddress = new UriBuilder(address) { Host = "localhost" }.Uri;
                application.Configuration = application.Services.GetRequiredService<IConfiguration>();
                application.Configuration["SeleniumTest:Local:BaseUrl"] = application.BaseAddress.AbsoluteUri;
                application.Configuration["Easypay:BaseUrl"] = application.BaseAddress.GetLeftPart(UriPartial.Authority);
                await application.SeedAsync();
                return application;
            }
            catch
            {
                await application.DisposeAsync();
                throw;
            }
        }

        /// <summary>Expires authentic Identity tokens deterministically for this test host only.</summary>
        public void ExpireIdentityTokens()
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            this.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan = TimeSpan.FromDays(-1);
        }

        /// <summary>Rejects verification or cleanup against anything but this host's memory database.</summary>
        /// <param name="context">Context used by a browser assertion or fixture cleanup.</param>
        public void AssertOwnsDatabase(ApplicationDbContext context)
        {
            if (this.disposed || context is not BrowserTestDbContext || !context.Database.IsSqlite()
                || context.Database.GetConnectionString() != this.applicationAnchor.ConnectionString)
            {
                throw new InvalidOperationException("Browser fixtures require this host's owned SQLite memory database.");
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            try
            {
                await this.factory.DisposeAsync();
            }
            finally
            {
                await this.applicationAnchor.DisposeAsync();
                await this.infrastructureAnchor.DisposeAsync();
                this.uploads.Dispose();
            }
        }

        private static Dictionary<string, string> CreateSettings(string identity, int smtpPort)
        {
            string password = "Browser-" + Guid.NewGuid().ToString("N") + "!9aA";
            return new Dictionary<string, string>
            {
                ["urls"] = "http://localhost:0",
                ["AllowedHosts"] = "localhost;127.0.0.1",
                ["LocalEmailCapture:Enabled"] = "true",
                ["LocalEmailCapture:UseInMemoryInfrastructure"] = "true",
                ["LocalEmailCapture:ExpectedDatabase"] = "browser_selenium",
                ["LocalEmailCapture:SmtpPort"] = smtpPort.ToString(CultureInfo.InvariantCulture),

                // Existing SMTP capture policy validates this synthetic catalog. SQL registrations
                // are removed below: the test application never opens this connection.
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1,1;Database=browser_selenium;Connect Timeout=1",
                ["ConnectionStrings:Infrastructure"] = string.Empty,
                ["IsEmailEnabled"] = "true",
                ["EmailFrom"] = "local-tests@example.test",

                // Application Insights 3 validates its connection string even when capture disables export.
                ["APPINSIGHTS_CONNECTIONSTRING"] = "InstrumentationKey=00000000-0000-0000-0000-000000000001;IngestionEndpoint=http://127.0.0.1:1/",
                ["Email.ConfirmPaymentNoInvoice.Subject"] = "Banco Alimentar: Confirmamos o pagamento da sua doação",
                ["Email.ConfirmPaymentNoInvoice.Body.Path"] = "Email.ConfirmPaymentNoInvoice.htm",
                ["Tenant-Override:Name"] = "alimentestaideia",
                ["Tenant-Override:DomainIdentifier"] = "localhost",
                ["Tenant-Override:InvoicingStrategy"] = "SingleInvoiceTable",
                ["Tenant-Override:PaymentStrategy"] = "SharedPaymentProcessor",
                ["Tenant-Override:UseSecrets"] = "true",
                ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureStorage:FoodBankSourceBlobName"] = string.Empty,
                ["AzureStorage:FoodBankSourceContainerName"] = string.Empty,
                ["IntegrationTesting:SkipAzureTableAuditing"] = "true",
                ["Easypay:BaseUrl"] = "http://127.0.0.1:1",
                ["Easypay:AccountId"] = "browser-test-account",
                ["Easypay:ApiKey"] = "browser-test-key",
                ["PayPal:clientId"] = "browser-test",
                ["PayPal:clientSecret"] = "browser-test",
                ["FunctionExecutionReports:Enabled"] = "false",
                ["FunctionExecutionReports:ConnectionString"] = string.Empty,
                ["FeatureManagement:ReferralCampaignManagement"] = "true",
                ["FeatureManagement:SubscriptionDonation"] = "true",
                ["FeatureManagement:EnableMaintenance"] = "false",
                ["SeleniumTest:Target"] = "Local",
                ["SeleniumTest:Local:SelfContained"] = "true",
                ["SeleniumTest:Local:Site:Username"] = "browser-site-" + identity + "@example.test",
                ["SeleniumTest:Local:Site:Password"] = password,
                ["SeleniumTest:Local:Admin:Username"] = "browser-admin-" + identity + "@example.test",
                ["SeleniumTest:Local:Admin:Password"] = password,
                ["SeleniumTest:Local:ReferralMetricsCode"] = "browsermetrics",
            };
        }

        private async Task SeedAsync()
        {
            using var scope = this.Services.CreateScope();
            var infrastructure = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            await infrastructure.Database.EnsureCreatedAsync();
            var tenant = IntegrationTestDataSeeder.CreateDefaultTenant();
            tenant.Domains.Single().Environment = "Development";
            infrastructure.Tenants.Add(tenant);
            await infrastructure.SaveChangesAsync();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();
            await InitDatabase.Seed(
                context,
                scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>(),
                scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(),
                this.Configuration);
            var owner = await IntegrationTestDataSeeder.EnsureUserAsync(
                scope.ServiceProvider,
                this.Configuration["SeleniumTest:Local:Site:Username"],
                this.Configuration["SeleniumTest:Local:Site:Password"]);
            await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                scope.ServiceProvider,
                this.Configuration["SeleniumTest:Local:Admin:Username"],
                this.Configuration["SeleniumTest:Local:Admin:Password"]);
            var referral = new Referral
            {
                Code = this.Configuration["SeleniumTest:Local:ReferralMetricsCode"],
                Name = "Campanha local de teste",
                TagLine = "Fixture de testes",
                Active = true,
                User = owner,
                CreateDate = DateTime.UtcNow,
                ImageUrl = new Uri(this.BaseAddress, "img/logo1.png").AbsoluteUri,
                LinkOpenCount = 12,
            };
            context.Referrals.Add(referral);
            var campaign = await context.Campaigns.FirstAsync();
            var foodBank = await context.FoodBanks.FirstAsync();
            for (int index = 0; index < 3; index++)
            {
                context.Donations.Add(new Donation
                {
                    PublicId = Guid.NewGuid(),
                    User = owner,
                    FoodBank = foodBank,
                    Campaign = campaign,
                    ReferralEntity = referral,
                    DonationAmount = index == 2 ? 99 : 12.5,
                    PaymentStatus = index == 2 ? PaymentStatus.WaitingPayment : PaymentStatus.Payed,
                    DonationDate = DateTime.UtcNow.AddDays(-3 + index),
                });
            }

            await context.SaveChangesAsync();
        }

        private sealed class BrowserFactory : WebApplicationFactory<Program>
        {
            private readonly Dictionary<string, string> settings;
            private readonly string applicationConnection;
            private readonly string infrastructureConnection;
            private readonly BrowserTestUploads uploads;

            public BrowserFactory(Dictionary<string, string> settings, string applicationConnection, string infrastructureConnection, BrowserTestUploads uploads)
            {
                this.settings = settings;
                this.applicationConnection = applicationConnection;
                this.infrastructureConnection = infrastructureConnection;
                this.uploads = uploads;
            }

            protected override IHostBuilder CreateHostBuilder()
            {
                return Host.CreateDefaultBuilder(Array.Empty<string>())
                    .UseEnvironment("Development")
                    .ConfigureAppConfiguration((_, configuration) =>
                    {
                        configuration.Sources.Clear();
                        configuration.AddInMemoryCollection(this.settings);
                        LocalEmailCapture.ValidateStartup(configuration.Build(), "Development", "http://localhost:0");
                    })
                    .ConfigureLogging(logging => logging.ClearProviders())
                    .ConfigureWebHostDefaults(web => web.UseStartup<Startup>()
                        .UseDefaultServiceProvider(options =>
                        {
                            options.ValidateOnBuild = false;
                            options.ValidateScopes = false;
                        }));
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting(WebHostDefaults.PreventHostingStartupKey, bool.TrueString);
                builder.ConfigureServices(services =>
                {
                    services.AddTransient<IStartupFilter, BrowserTestStaticFiles>();
                    services.AddSingleton(this.uploads);
                    services.RemoveAll<ReferralImageService>();
                    services.AddScoped(provider => this.uploads.CreateImageService(provider.GetRequiredService<IWebHostEnvironment>()));
                    services.AddSingleton<BrowserTestPayments>();
                    services.AddSingleton<IStartupFilter>(provider => provider.GetRequiredService<BrowserTestPayments>());
                    services.AddSingleton(_ => new BrowserTestDonationDiagnostics());
                    services.Configure<MvcOptions>(options => options.Filters.AddService<BrowserTestDonationDiagnostics>());
                    services.RemoveAll<PayPalBuilder>();
                    services.AddScoped(provider =>
                    {
                        var paypal = new PayPalBuilder(provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
                        paypal.SetPayPalHttpClientOverride(new StubPayPalHttpClient(_ => provider.GetRequiredService<BrowserTestPayments>().RejectPayPalAsync()));
                        return paypal;
                    });
                    RemoveDatabase<ApplicationDbContext>(services);
                    RemoveDatabase<InfrastructureDbContext>(services);
                    services.AddScoped<ApplicationDbContext>(_ => new BrowserTestDbContext(
                        new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(this.applicationConnection).Options));
                    services.AddScoped<InfrastructureDbContext>(_ => new InfrastructureDbContext(
                        new DbContextOptionsBuilder<InfrastructureDbContext>().UseSqlite(this.infrastructureConnection).Options));
                    services.RemoveAll<IKeyVaultConfigurationManager>();
                    services.AddTransient<IKeyVaultConfigurationManager, TestKeyVaultConfigurationManager>();
                    foreach (var service in services.Where(value => value.ServiceType == typeof(IHostedService)
                        && value.ImplementationType == typeof(TenantStaticSyncHostedService)).ToList())
                    {
                        services.Remove(service);
                    }

                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.Configure<TelemetryConfiguration>(configuration => configuration.DisableTelemetry = true);
                    services.PostConfigure<AuthorizationOptions>(options =>
                    {
                        // No external OAuth credentials are loaded by this host. Retain every
                        // production policy requirement, but authenticate through the real local cookie.
                        foreach (string name in new[] { "AdminArea", "RoleArea" })
                        {
                            AuthorizationPolicy policy = options.GetPolicy(name);
                            options.AddPolicy(name, new AuthorizationPolicy(policy.Requirements, new[] { IdentityConstants.ApplicationScheme }));
                        }
                    });
                });
            }

            private static void RemoveDatabase<TContext>(IServiceCollection services)
                where TContext : DbContext
            {
                services.RemoveAll<TContext>();
                services.RemoveAll<DbContextOptions<TContext>>();
                services.RemoveAll<IDbContextFactory<TContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<TContext>>();
            }
        }
    }
}
