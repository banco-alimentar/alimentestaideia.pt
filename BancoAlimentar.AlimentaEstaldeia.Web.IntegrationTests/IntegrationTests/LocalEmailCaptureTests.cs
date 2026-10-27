// -----------------------------------------------------------------------
// <copyright file="LocalEmailCaptureTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Mail;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core.Pipeline;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using Xunit;

    /// <summary>
    /// Tests local capture policy and the real mail transport without external services.
    /// </summary>
    [Trait("Category", "Offline")]
    public class LocalEmailCaptureTests
    {
        private const string PasswordMarker = "synthetic-capture-password";

        /// <summary>Fresh Identity sessions omit browser telemetry in every capture-mode layout.</summary>
        /// <param name="layout">Tenant or fallback Razor layout.</param>
        /// <param name="captureEnabled">Whether the local capture profile is active.</param>
        /// <returns>A task representing rendered HTML assertions.</returns>
        [Theory]
        [InlineData("/Pages/Shared/_Layout.cshtml", true)]
        [InlineData("/Pages/Shared/_Layout.cshtml", false)]
        [InlineData("/Pages/Tenants/alimentestaideia/Pages/_Layout.cshtml", true)]
        [InlineData("/Pages/Tenants/alimentestaideia/Pages/_Layout.cshtml", false)]
        [InlineData("/Pages/Tenants/BancoAlimentar/Pages/_Layout.cshtml", true)]
        [InlineData("/Pages/Tenants/BancoAlimentar/Pages/_Layout.cshtml", false)]
        public async Task IdentityPages_OmitBrowserTelemetryWhenCapturing(string layout, bool captureEnabled)
        {
            using var factory = new CustomWebApplicationFactory();
            using var webFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["LocalEmailCapture:Enabled"] = captureEnabled.ToString(CultureInfo.InvariantCulture),
                    ["LocalEmailCapture:ExpectedDatabase"] = "app_selenium",
                    ["LocalEmailCapture:ExpectedInfrastructureDatabase"] = "infrastructure_selenium",
                    ["ConnectionStrings:DefaultConnection"] = ConnectionString("app_selenium"),
                    ["ConnectionStrings:Infrastructure"] = ConnectionString("infrastructure_selenium"),
                    ["urls"] = "http://localhost:5050",
                }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(ITenantLayout));
                    services.AddSingleton<ITenantLayout>(new TestLayout(layout));
                    services.Configure<TelemetryConfiguration>(telemetry => telemetry.DisableTelemetry = true);
                });
            });
            using var client = webFactory.CreateClient();
            using var response = await client.GetAsync("/Identity/Account/ForgotPassword");
            response.EnsureSuccessStatusCode();
            string html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Input_Email", html);
            Assert.Equal(!captureEnabled, html.Contains("js.monitor.azure.com", StringComparison.Ordinal));
            Assert.Equal(!captureEnabled, html.Contains("n.trackPageView", StringComparison.Ordinal));
        }

        /// <summary>Capture mode suppresses requests even when telemetry credentials are inherited.</summary>
        /// <param name="captureEnabled">Whether the local capture profile is active.</param>
        /// <param name="expectedRequests">Expected requests reaching the test channel.</param>
        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 1)]
        public void Telemetry_InheritedCredentialsCannotExportLocalResetTokens(bool captureEnabled, int expectedRequests)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:Enabled"] = captureEnabled.ToString(CultureInfo.InvariantCulture);
            using var exporter = new CountingRequestExporter();
            using var transport = new HttpClient(new NoNetworkTelemetryHandler());
            using var telemetry = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001;IngestionEndpoint=https://inherited.telemetry.invalid/",
                SamplingRatio = 1,
                EnableLiveMetrics = false,
                DisableOfflineStorage = true,
            };
            telemetry.ConfigureOpenTelemetryBuilder(builder =>
            {
                builder.WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(exporter)));

                // Keep the real SDK/export pipeline, but never let this offline test send HTTP.
                builder.Services.Configure<AzureMonitorExporterOptions>(options => options.Transport = new HttpClientTransport(transport));
            });
            LocalEmailCapture.ConfigureTelemetry(configuration, telemetry);
            var client = new TelemetryClient(telemetry);
            client.TrackRequest(new RequestTelemetry
            {
                Name = exporter.RequestName,
                Url = new Uri("https://localhost:5051/Identity/Account/ResetPassword?code=synthetic-token"),
                ResponseCode = "200",
                Success = true,
            });
            Assert.Equal(captureEnabled, telemetry.DisableTelemetry);
            Assert.Equal(expectedRequests, exporter.RequestCount);
        }

        /// <summary>A local browser request must not initialize or migrate an unprepared schema.</summary>
        /// <returns>A task representing the schema guard assertion.</returns>
        [Fact]
        public async Task TenantInitialization_RequiresExplicitLocalMigrations()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Program).Assembly.FullName))
                .Options;
            using var context = new ApplicationDbContext(options);
            Assert.NotEmpty(context.Database.GetPendingMigrations());
            var services = new ServiceCollection();
            services.AddSingleton(context);
            using var provider = services.BuildServiceProvider();
            var request = new DefaultHttpContext { RequestServices = provider };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new TenantInitializationService()
                .InitializeTenant(request, new Tenant { Id = 42 }, null, CreateConfiguration()));

            Assert.Contains("Apply migrations explicitly", exception.Message);
            Assert.Empty(context.Database.GetAppliedMigrations());
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'";
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync()));
        }

        /// <summary>Disabled capture leaves existing environments untouched.</summary>
        /// <param name="environment">Hosting environment.</param>
        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void DisabledCapture_DoesNotChangeTransport(string environment)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:Enabled"] = "false";
            LocalEmailCapture.ValidateStartup(configuration, environment, "http://public.example.test");
            Assert.Null(LocalEmailCapture.CreateClient(configuration, environment));
        }

        /// <summary>Only explicit loopback application listeners are accepted.</summary>
        /// <param name="urls">Local listening URLs.</param>
        [Theory]
        [InlineData("http://localhost:5050;https://localhost:5051")]
        [InlineData("http://127.0.0.1:5050")]
        [InlineData("http://[::1]:5050")]
        public void Startup_AcceptsIsolatedLoopbackApplication(string urls)
        {
            LocalEmailCapture.ValidateStartup(CreateConfiguration(), "Development", urls);
        }

        /// <summary>In-memory infrastructure needs neither a SQL connection nor an expected catalog.</summary>
        [Fact]
        public void InMemoryInfrastructure_AcceptsMissingInfrastructureConfiguration()
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:UseInMemoryInfrastructure"] = "true";
            configuration["LocalEmailCapture:ExpectedInfrastructureDatabase"] = null;
            configuration["ConnectionStrings:Infrastructure"] = null;

            LocalEmailCapture.ValidateStartup(configuration, "Development", "http://localhost:5050");
            LocalEmailCapture.ValidateDataAccess(configuration, "Development");
            using var client = LocalEmailCapture.CreateClient(configuration, "Development");
            Assert.NotNull(client);
            Assert.Equal("127.0.0.1", client.Host);
        }

        /// <summary>Unused inherited infrastructure settings cannot block local capture.</summary>
        /// <param name="connectionString">Inherited infrastructure connection.</param>
        /// <param name="expectedDatabase">Unused expected infrastructure catalog.</param>
        [Theory]
        [InlineData("", "")]
        [InlineData("Initial Catalog=alimentestaideia.core", null)]
        [InlineData("Initial Catalog=alimentestaideia.core_staging", "alimentestaideia.core_staging")]
        [InlineData("UnsupportedKey=synthetic-capture-password", "infrastructure_selenium")]
        [InlineData("#{InfrastructureConnection}#", "not-an-isolated-database")]
        public void InMemoryInfrastructure_IgnoresInheritedInfrastructureSettings(string connectionString, string expectedDatabase)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:UseInMemoryInfrastructure"] = "true";
            configuration["LocalEmailCapture:ExpectedInfrastructureDatabase"] = expectedDatabase;
            configuration["ConnectionStrings:Infrastructure"] = connectionString;

            LocalEmailCapture.ValidateStartup(configuration, "Development", "http://localhost:5050");
            LocalEmailCapture.ValidateDataAccess(configuration, "Development");
            using var client = LocalEmailCapture.CreateClient(configuration, "Development");
            Assert.NotNull(client);
        }

        /// <summary>In-memory infrastructure never relaxes isolation of the application SQL database.</summary>
        /// <param name="connectionString">Unsafe application connection.</param>
        /// <param name="expectedDatabase">Configured application catalog.</param>
        [Theory]
        [InlineData(null, "app_selenium")]
        [InlineData("UnsupportedKey=synthetic-capture-password", "app_selenium")]
        [InlineData("Initial Catalog=alimentestaideia.core", "app_selenium")]
        [InlineData("Initial Catalog=alimentestaideia.core_staging", "app_selenium")]
        [InlineData("Initial Catalog=different_selenium", "app_selenium")]
        [InlineData("Initial Catalog=app_selenium", null)]
        [InlineData("Initial Catalog=app_selenium", "_selenium")]
        [InlineData("Initial Catalog=alimentestaideia.core", "alimentestaideia.core")]
        public void InMemoryInfrastructure_RejectsUnsafeApplicationDatabase(string connectionString, string expectedDatabase)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:UseInMemoryInfrastructure"] = "true";
            configuration["ConnectionStrings:DefaultConnection"] = connectionString;
            configuration["LocalEmailCapture:ExpectedDatabase"] = expectedDatabase;

            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateStartup(configuration, "Development", "http://localhost:5050"));
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateDataAccess(configuration, "Development"));
            var exception = Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, "Development"));
            Assert.DoesNotContain(PasswordMarker, exception.ToString());
        }

        /// <summary>In-memory infrastructure remains restricted to Development capture.</summary>
        /// <param name="environment">Non-development environment.</param>
        [Theory]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void InMemoryInfrastructure_RejectsNonDevelopmentCapture(string environment)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:UseInMemoryInfrastructure"] = "true";
            configuration["ConnectionStrings:Infrastructure"] = null;
            configuration["LocalEmailCapture:ExpectedInfrastructureDatabase"] = null;

            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateStartup(configuration, environment, "http://localhost:5050"));
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateDataAccess(configuration, environment));
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, environment));
        }

        /// <summary>Tenant configuration cannot select or disable the process infrastructure mode.</summary>
        /// <param name="rootMode">Process infrastructure switch.</param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InMemoryInfrastructureMode_IgnoresTenantOverrides(bool rootMode)
        {
            var root = CreateConfiguration();
            root["LocalEmailCapture:UseInMemoryInfrastructure"] = rootMode.ToString(CultureInfo.InvariantCulture);
            root["ConnectionStrings:Infrastructure"] = "UnsupportedKey=" + PasswordMarker;
            var configuration = CreateTenantConfiguration(root, new Dictionary<string, string>
            {
                ["LocalEmailCapture:UseInMemoryInfrastructure"] = (!rootMode).ToString(CultureInfo.InvariantCulture),
            });

            Assert.Equal(rootMode, LocalEmailCapture.UsesInMemoryInfrastructure(configuration));
            if (rootMode)
            {
                LocalEmailCapture.ValidateStartup(configuration, "Development", "http://localhost:5050");
                LocalEmailCapture.ValidateDataAccess(configuration, "Development");
                using var client = LocalEmailCapture.CreateClient(configuration, "Development");
                Assert.NotNull(client);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateStartup(configuration, "Development", "http://localhost:5050"));
                Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateDataAccess(configuration, "Development"));
                Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, "Development"));
            }
        }

        /// <summary>Disabled capture ignores the infrastructure flag and retains the normal transport.</summary>
        /// <param name="environment">Hosting environment.</param>
        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void InMemoryInfrastructureFlag_DoesNotEnableDisabledCapture(string environment)
        {
            var root = CreateConfiguration();
            root["LocalEmailCapture:Enabled"] = "false";
            root["LocalEmailCapture:UseInMemoryInfrastructure"] = "true";
            root["ConnectionStrings:DefaultConnection"] = "UnsupportedKey=" + PasswordMarker;
            root["ConnectionStrings:Infrastructure"] = "UnsupportedKey=" + PasswordMarker;
            var configuration = CreateTenantConfiguration(root, new Dictionary<string, string>
            {
                ["LocalEmailCapture:Enabled"] = "true",
            });

            Assert.False(LocalEmailCapture.UsesInMemoryInfrastructure(configuration));
            LocalEmailCapture.ValidateStartup(configuration, environment, "http://public.example.test");
            LocalEmailCapture.ValidateDataAccess(configuration, environment);
            Assert.Null(LocalEmailCapture.CreateClient(configuration, environment));
        }

        /// <summary>The real Startup registration opens only SQLite and seeds the development tenant.</summary>
        /// <param name="inheritedConnection">Unused inherited SQL connection.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("Server=127.0.0.1,1;Initial Catalog=alimentestaideia.core;Connect Timeout=1")]
        [InlineData("UnsupportedKey=synthetic-capture-password")]
        public async Task Startup_InMemoryInfrastructureResolvesSqliteAndSeedsTenant(string inheritedConnection)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:UseInMemoryInfrastructure"] = "true";
            configuration["ConnectionStrings:Infrastructure"] = inheritedConnection;
            configuration["LocalEmailCapture:ExpectedInfrastructureDatabase"] = null;
            configuration["Tenant-Override:Name"] = "local-capture-tenant";
            configuration["Tenant-Override:DomainIdentifier"] = "localhost";
            configuration["Tenant-Override:InvoicingStrategy"] = "SingleInvoiceTable";
            configuration["Tenant-Override:PaymentStrategy"] = "SharedPaymentProcessor";
            using var provider = CreateStartupServices(configuration, "Development");
            using var scope = provider.CreateScope();
            var infrastructure = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            using var connection = infrastructure.Database.GetDbConnection();

            Assert.True(infrastructure.Database.IsSqlite(), "Startup must ignore inherited SQL infrastructure connections.");
            Assert.Equal(":memory:", new SqliteConnectionStringBuilder(connection.ConnectionString).DataSource);
            Assert.Equal(ConnectionState.Open, connection.State);
            var tenant = Assert.Single(await infrastructure.Tenants.Include(value => value.Domains).AsNoTracking().ToListAsync());
            Assert.Equal("local-capture-tenant", tenant.Name);
            Assert.Equal("localhost", Assert.Single(tenant.Domains).DomainName);
            Assert.Equal(InvoicingStrategy.SingleInvoiceTable, tenant.InvoicingStrategy);
            Assert.Equal(PaymentStrategy.SharedPaymentProcessor, tenant.PaymentStrategy);
            Assert.NotEqual(Guid.Empty, tenant.PublicId);

            var application = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(application.Database.IsSqlServer(), "Only infrastructure may use SQLite.");
            Assert.Equal("app_selenium", application.Database.GetDbConnection().Database);
            Assert.Equal(ConnectionState.Closed, application.Database.GetDbConnection().State);
        }

        /// <summary>Other modes retain SQL infrastructure without opening a connection.</summary>
        /// <param name="environment">Hosting environment.</param>
        /// <param name="captureEnabled">Process capture switch.</param>
        /// <param name="inMemoryInfrastructure">Process infrastructure switch.</param>
        [Theory]
        [InlineData("Development", false, true)]
        [InlineData("Development", true, false)]
        [InlineData("Staging", true, true)]
        [InlineData("Production", true, true)]
        public void Startup_InfrastructureSqlRegistrationRemainsUnchanged(string environment, bool captureEnabled, bool inMemoryInfrastructure)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:Enabled"] = captureEnabled.ToString(CultureInfo.InvariantCulture);
            configuration["LocalEmailCapture:UseInMemoryInfrastructure"] = inMemoryInfrastructure.ToString(CultureInfo.InvariantCulture);
            configuration["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true";
            configuration["DataProtectionKeyVaultKey"] = "https://capture-test.vault.azure.net/keys/test/version";
            using var provider = CreateStartupServices(configuration, environment);
            using var scope = provider.CreateScope();
            var infrastructure = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();

            Assert.True(infrastructure.Database.IsSqlServer());
            Assert.Equal("infrastructure_selenium", infrastructure.Database.GetDbConnection().Database);
            Assert.Equal(ConnectionState.Closed, infrastructure.Database.GetDbConnection().State);
        }

        /// <summary>Azure hosting environments cannot opt into local capture.</summary>
        /// <param name="environment">Non-local hosting environment.</param>
        [Theory]
        [InlineData("Staging")]
        [InlineData("Production")]
        public void Capture_RejectsNonDevelopmentEnvironment(string environment)
        {
            var configuration = CreateConfiguration();
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateStartup(configuration, environment, "http://localhost:5050"));
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, environment));
        }

        /// <summary>Wildcard, public, and ambiguous listening addresses fail closed.</summary>
        /// <param name="urls">Unsafe listening configuration.</param>
        [Theory]
        [InlineData("")]
        [InlineData("http://0.0.0.0:5050")]
        [InlineData("http://+:5050")]
        [InlineData("https://dev.alimentestaideia.pt")]
        [InlineData("http://localhost:5050;https://example.test")]
        [InlineData("http://user@localhost:5050")]
        [InlineData("http://localhost:5050/path")]
        [InlineData("http://localhost:5050?secret=synthetic")]
        [InlineData("http://localhost:5050#fragment")]
        public void Startup_RejectsUnsafeListeners(string urls)
        {
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateStartup(CreateConfiguration(), "Development", urls));
        }

        /// <summary>Public Kestrel endpoints cannot override otherwise safe listening URLs.</summary>
        [Fact]
        public void Startup_RejectsPublicKestrelEndpoint()
        {
            var configuration = CreateConfiguration();
            configuration["Kestrel:Endpoints:Public:Url"] = "http://0.0.0.0:8080";
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateStartup(configuration, "Development", "http://localhost:5050"));
        }

        /// <summary>Neither tenant nor infrastructure access can use a shared database.</summary>
        /// <param name="key">Effective database connection key.</param>
        /// <param name="catalog">Unsafe or mismatched database name.</param>
        [Theory]
        [InlineData("DefaultConnection", "alimentestaideia.core")]
        [InlineData("DefaultConnection", "alimentestaideia.core_staging")]
        [InlineData("DefaultConnection", "different_selenium")]
        [InlineData("Infrastructure", "alimentestaideia.core")]
        [InlineData("Infrastructure", "alimentestaideia.core_staging")]
        [InlineData("Infrastructure", "different_selenium")]
        public void Capture_RejectsMismatchedDatabasesBeforeAccess(string key, string catalog)
        {
            var configuration = CreateConfiguration();
            configuration["ConnectionStrings:" + key] = ConnectionString(catalog);
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateDataAccess(configuration, "Development"));
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, "Development"));
        }

        /// <summary>Expected database configuration must identify an isolated test database.</summary>
        /// <param name="expected">Invalid expected catalog.</param>
        [Theory]
        [InlineData("")]
        [InlineData("_selenium")]
        [InlineData("alimentestaideia.core")]
        [InlineData("alimentestaideia.core_staging")]
        public void Capture_RejectsNonTestExpectedDatabase(string expected)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:ExpectedDatabase"] = expected;
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, "Development"));
        }

        /// <summary>SQL parser errors never expose connection-string credentials.</summary>
        /// <param name="key">Malformed connection key.</param>
        [Theory]
        [InlineData("DefaultConnection")]
        [InlineData("Infrastructure")]
        public void Capture_RedactsMalformedConnectionErrors(string key)
        {
            var configuration = CreateConfiguration();
            configuration["ConnectionStrings:" + key] = "UnsupportedKey=" + PasswordMarker;
            var exception = Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, "Development"));
            Assert.DoesNotContain(PasswordMarker, exception.ToString());
            Assert.Null(exception.InnerException);
        }

        /// <summary>Tenant overrides cannot enable, disable, or redirect process-level capture.</summary>
        [Fact]
        public void CaptureSwitchAndTransport_IgnoreTenantOverrides()
        {
            IConfiguration root = CreateConfiguration();
            var configuration = CreateTenantConfiguration(root, new Dictionary<string, string>
            {
                ["LocalEmailCapture:Enabled"] = "false",
                ["LocalEmailCapture:SmtpPort"] = "25",
                ["Smtp:Host"] = "smtp.example.test",
                ["Smtp:UseCredentials"] = "true",
            });
            Assert.True(LocalEmailCapture.IsEnabled(configuration));
            using var client = LocalEmailCapture.CreateClient(configuration, "Development");
            Assert.Equal("127.0.0.1", client.Host);
            Assert.Equal(1025, client.Port);
            Assert.False(client.EnableSsl);
            Assert.False(client.UseDefaultCredentials);
            Assert.Null(client.Credentials);
            Assert.Equal(SmtpDeliveryMethod.Network, client.DeliveryMethod);

            root["LocalEmailCapture:Enabled"] = "false";
            configuration = CreateTenantConfiguration(root, new Dictionary<string, string> { ["LocalEmailCapture:Enabled"] = "true" });
            Assert.False(LocalEmailCapture.IsEnabled(configuration));
            Assert.Null(LocalEmailCapture.CreateClient(configuration, "Production"));
        }

        /// <summary>Effective tenant SQL overrides cannot bypass the isolated database guard.</summary>
        [Fact]
        public void TenantDatabaseOverride_IsRejectedBeforeAccess()
        {
            var configuration = CreateTenantConfiguration(CreateConfiguration(), new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString("alimentestaideia.core"),
                ["LocalEmailCapture:ExpectedDatabase"] = "alimentestaideia.core",
            });
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.ValidateDataAccess(configuration, "Development"));
        }

        /// <summary>Invalid capture ports cannot select another transport or service implicitly.</summary>
        /// <param name="port">Invalid TCP port.</param>
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(65536)]
        public void Capture_RejectsInvalidPorts(int port)
        {
            var configuration = CreateConfiguration();
            configuration["LocalEmailCapture:SmtpPort"] = port.ToString(CultureInfo.InvariantCulture);
            Assert.Throws<InvalidOperationException>(() => LocalEmailCapture.CreateClient(configuration, "Development"));
        }

        /// <summary>
        /// Local capture overrides both a disabled tenant mail switch and tenant SMTP transport.
        /// </summary>
        /// <returns>A task representing the transport assertion.</returns>
        /// <param name="inMemoryInfrastructure">Whether infrastructure SQL is bypassed.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendMail_CapturesWhenTenantDisablesEmailAndOverridesSmtp(bool inMemoryInfrastructure)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            IConfiguration root = CreateConfiguration();
            root["LocalEmailCapture:UseInMemoryInfrastructure"] = inMemoryInfrastructure.ToString(CultureInfo.InvariantCulture);
            if (inMemoryInfrastructure)
            {
                root["ConnectionStrings:Infrastructure"] = "UnsupportedKey=" + PasswordMarker;
                root["LocalEmailCapture:ExpectedInfrastructureDatabase"] = null;
            }

            root["LocalEmailCapture:SmtpPort"] = port.ToString(CultureInfo.InvariantCulture);
            root["EmailFrom"] = "sender@example.invalid";
            IConfiguration configuration = CreateTenantConfiguration(root, new Dictionary<string, string>
            {
                ["LocalEmailCapture:Enabled"] = "false",
                ["LocalEmailCapture:SmtpPort"] = "1",
                ["IsEmailEnabled"] = "false",
                ["Smtp:Host"] = "smtp.tenant.invalid",

                // An invalid tenant port makes an accidental legacy transport fail before DNS/network access.
                ["Smtp:Port"] = "-1",
                ["Smtp:EnableSsl"] = "true",
                ["Smtp:UseCredentials"] = "true",
                ["Smtp:User"] = "tenant-user",
                ["Smtp:Password"] = PasswordMarker,
            });
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            using ServiceProvider provider = services.BuildServiceProvider();
            using var telemetryConfiguration = new TelemetryConfiguration { DisableTelemetry = true };
            var telemetry = new TelemetryClient(telemetryConfiguration);
            var environment = new LocalWebHostEnvironment();
            var mail = new Mail(
                null,
                environment,
                configuration,
                null,
                null,
                telemetry,
                environment,
                null,
                null,
                provider.GetRequiredService<IServiceScopeFactory>());
            Task<string> capture = ReadSmtpMessageAsync(listener, timeout.Token);
            try
            {
                bool sent = mail.SendMail(
                    "capture-body-marker",
                    "capture-subject-marker",
                    "recipient@example.invalid",
                    null,
                    null,
                    configuration);

                Assert.True(sent, "The real mail transport must accept local capture.");
                Task completed = await Task.WhenAny(capture, Task.Delay(TimeSpan.FromSeconds(3), timeout.Token));
                Assert.True(completed == capture, "Local capture did not receive the message through SMTP.");
                string message = await capture;
                Assert.Contains("recipient@example.invalid", message);
                Assert.Contains("capture-subject-marker", message);
                Assert.True(
                    message.Contains("capture-body-marker", StringComparison.Ordinal)
                    || message.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("capture-body-marker")), StringComparison.Ordinal),
                    "SMTP DATA must contain the supplied body.");
                Assert.DoesNotContain(PasswordMarker, message);
            }
            finally
            {
                timeout.Cancel();
                listener.Stop();
                try
                {
                    await capture;
                }
                catch (OperationCanceledException)
                {
                    // Expected when the legacy disabled-email shortcut never opens a connection.
                }
                catch (SocketException)
                {
                    // Stopping the listener also releases a pending accept on supported runtimes.
                }
            }
        }

        private static async Task<string> ReadSmtpMessageAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken);
            using NetworkStream stream = connection.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
            using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true) { NewLine = "\r\n", AutoFlush = true };
            await writer.WriteLineAsync("220 localhost SMTP capture".AsMemory(), cancellationToken);
            var message = new StringBuilder();
            bool readingData = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                string line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }

                if (readingData)
                {
                    if (line == ".")
                    {
                        await writer.WriteLineAsync("250 captured".AsMemory(), cancellationToken);
                        return message.ToString();
                    }

                    message.AppendLine(line);
                }
                else if (line.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("HELO ", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250 localhost".AsMemory(), cancellationToken);
                }
                else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    readingData = true;
                    await writer.WriteLineAsync("354 end with a dot".AsMemory(), cancellationToken);
                }
                else
                {
                    await writer.WriteLineAsync("500 unsupported command".AsMemory(), cancellationToken);
                }
            }

            return string.Empty;
        }

        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["LocalEmailCapture:Enabled"] = "true",
                ["LocalEmailCapture:ExpectedDatabase"] = "app_selenium",
                ["LocalEmailCapture:ExpectedInfrastructureDatabase"] = "infrastructure_selenium",
                ["ConnectionStrings:DefaultConnection"] = ConnectionString("app_selenium"),
                ["ConnectionStrings:Infrastructure"] = ConnectionString("infrastructure_selenium"),
            }).Build();
        }

        private static ServiceProvider CreateStartupServices(IConfiguration configuration, string environmentName)
        {
            var environment = new LocalWebHostEnvironment
            {
                EnvironmentName = environmentName,
                ApplicationName = typeof(BancoAlimentar.AlimentaEstaIdeia.Web.Startup).Assembly.GetName().Name,
            };
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRouting();
            services.AddSingleton(configuration);
            services.AddSingleton<IWebHostEnvironment>(environment);
            services.AddSingleton<IHostEnvironment>(environment);
            new BancoAlimentar.AlimentaEstaIdeia.Web.Startup(configuration, environment).ConfigureServices(services);

            // This fixture exercises database registrations, not a running telemetry host.
            services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            return services.BuildServiceProvider();
        }

        private static string ConnectionString(string catalog)
        {
            return "Server=127.0.0.1,1;Initial Catalog=" + catalog + ";User ID=capture-test;Password=" + PasswordMarker + ";Connect Timeout=1";
        }

        private static IConfiguration CreateTenantConfiguration(IConfiguration root, Dictionary<string, string> overrides)
        {
            var context = new DefaultHttpContext();
            context.SetTenant(new Tenant { Id = 42, Name = "capture-test" });
            context.Items[typeof(IKeyVaultConfigurationManager).Name] = new TenantConfigurationManager(overrides);
            return new TenantConfigurationRoot(root, new HttpContextAccessor { HttpContext = context });
        }

        private sealed class TenantConfigurationManager : IKeyVaultConfigurationManager
        {
            private readonly Dictionary<string, string> values;

            public TenantConfigurationManager(Dictionary<string, string> values)
            {
                this.values = values;
            }

            public Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions) => Task.FromResult(true);

            public Dictionary<string, string> GetTenantConfiguration(int tenantId) => this.values;

            public bool LoadTenantConfiguration() => true;

            public KeyVaultConfigurationLoadDiagnostics GetLastLoadDiagnostics() => null;
        }

        private sealed class TestLayout : ITenantLayout
        {
            public TestLayout(string layout)
            {
                this.Layout = layout;
            }

            public string Layout { get; }

            public string AdminLayout => this.Layout;

            public string Debug => this.Layout;
        }

        private sealed class CountingRequestExporter : BaseExporter<Activity>
        {
            public string RequestName { get; } = "Local password reset " + Guid.NewGuid();

            public int RequestCount { get; private set; }

            public override ExportResult Export(in Batch<Activity> batch)
            {
                foreach (Activity activity in batch)
                {
                    if (activity.DisplayName == this.RequestName)
                    {
                        this.RequestCount++;
                    }
                }

                return ExportResult.Success;
            }
        }

        private sealed class NoNetworkTelemetryHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"itemsReceived\":1,\"itemsAccepted\":1,\"errors\":[]}"),
                });
            }
        }

        private sealed class LocalWebHostEnvironment : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Development";

            public string ApplicationName { get; set; } = "LocalEmailCaptureTests";

            public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

            public string WebRootPath { get; set; } = AppContext.BaseDirectory;

            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
