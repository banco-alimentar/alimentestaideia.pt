// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Integration.Configuration
{
    using Azure;
    using Azure.Core;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tenant default configuration integration tests.
    /// </summary>
    public class TenantConfigurationTest : IClassFixture<ServicesFixture>
    {
        private static int nextTenantId;
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantConfigurationTest"/> class.
        /// </summary>
        /// <param name="fixture">Shared in-memory services.</param>
        public TenantConfigurationTest(ServicesFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Local overrides load once and subsequent requests retain the cached values.
        /// </summary>
        [Fact]
        public async Task LocalConfigurationLoadsOnce()
        {
            int tenantId = Interlocked.Decrement(ref nextTenantId);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Test:Setting", "initial" } })
                .Build();
            var cache = new KeyVaultConfigurationCache();
            KeyVaultConfigurationManager manager = this.CreateManager(configuration, cache);
            var options = new TenantDevelopmentOptions { UseSecrets = true };

            Assert.Null(manager.GetTenantConfiguration(tenantId));
            Assert.True(await manager.EnsureTenantConfigurationLoaded(tenantId, options));
            Dictionary<string, string> secrets = manager.GetTenantConfiguration(tenantId);
            Assert.NotNull(secrets);
            Assert.Equal("initial", secrets["Test:Setting"]);

            configuration["Test:Setting"] = "changed";

            Assert.False(await manager.EnsureTenantConfigurationLoaded(tenantId, options));
            Assert.Same(secrets, manager.GetTenantConfiguration(tenantId));
            Assert.Equal("initial", secrets["Test:Setting"]);
        }

        /// <summary>
        /// Production configuration remains absent when no tenant client is registered.
        /// </summary>
        [Fact]
        public async Task ProductionTenantWithoutClientDoesNotLoadConfiguration()
        {
            int tenantId = Interlocked.Decrement(ref nextTenantId);
            KeyVaultConfigurationManager manager = this.CreateManager(new ConfigurationBuilder().Build(), new KeyVaultConfigurationCache());

            Assert.False(await manager.EnsureTenantConfigurationLoaded(tenantId, TenantDevelopmentOptions.ProductionOptions));
            Assert.Null(manager.GetTenantConfiguration(tenantId));
        }

        /// <summary>
        /// Managers share host configuration until reload invalidates the entire snapshot.
        /// </summary>
        [Fact]
        public async Task ConfigurationReloadIsSharedAcrossManagers()
        {
            int tenantId = Interlocked.Decrement(ref nextTenantId);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Test:Setting", "initial" } })
                .Build();
            var cache = new KeyVaultConfigurationCache();
            KeyVaultConfigurationManager first = this.CreateManager(configuration, cache);
            KeyVaultConfigurationManager second = this.CreateManager(configuration, cache);
            var options = new TenantDevelopmentOptions { UseSecrets = true };
            Assert.True(await first.EnsureTenantConfigurationLoaded(tenantId, options));
            Dictionary<string, string> original = first.GetTenantConfiguration(tenantId);
            configuration["Test:Setting"] = "changed";
            Assert.False(await second.EnsureTenantConfigurationLoaded(tenantId, options));
            Assert.Same(original, second.GetTenantConfiguration(tenantId));

            cache.Clear();

            Assert.Null(first.GetTenantConfiguration(tenantId));
            Assert.True(await second.EnsureTenantConfigurationLoaded(tenantId, options));
            Assert.Equal("changed", first.GetTenantConfiguration(tenantId)["Test:Setting"]);
            Assert.Equal("initial", original["Test:Setting"]);
        }

        /// <summary>
        /// A broken tenant client does not block healthy tenants and remains retryable.
        /// </summary>
        [Fact]
        public async Task BrokenTenantDoesNotBlockHealthyTenants()
        {
            using InfrastructureDbContext database = CreateVaultDatabase();
            var cache = new KeyVaultConfigurationCache();
            var clock = new TestClock();
            Mock<SecretClient> core = CreateSecretClient("Test--Core", () => Task.FromResult("core-value"));
            Mock<SecretClient> tenant = CreateSecretClient("Test--Setting", () => Task.FromResult("tenant-value"));
            int secondClientAttempts = 0;
            SecretClient Factory(Uri uri)
            {
                if (uri.Host == "second.vault.azure.net" && secondClientAttempts++ == 0)
                {
                    throw new IOException("Synthetic client initialization failure.");
                }

                return uri.Host == "doar-sas-core.vault.azure.net" ? core.Object : tenant.Object;
            }

            KeyVaultConfigurationManager first = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, Factory, clock: clock);
            Assert.True(await first.LoadTenantConfigurationAsync());
            Assert.True(await first.EnsureTenantConfigurationLoaded(1, TenantDevelopmentOptions.ProductionOptions));
            Assert.Equal("tenant-value", first.GetTenantConfiguration(1)["Test:Setting"]);
            Assert.False(await first.EnsureTenantConfigurationLoaded(2, TenantDevelopmentOptions.ProductionOptions));
            Assert.Null(first.GetTenantConfiguration(2));
            Assert.Equal("TenantKeyVaultInitializationFailed", cache.GetStatus(2).FailureStage);
            Assert.Equal(1, secondClientAttempts);

            clock.Advance(TimeSpan.FromMinutes(1));
            Assert.True(await first.EnsureTenantConfigurationLoaded(2, TenantDevelopmentOptions.ProductionOptions));
            Assert.Null(cache.GetStatus(2).FailureStage);
            Assert.Equal("tenant-value", first.GetTenantConfiguration(2)["Test:Setting"]);
            core.Verify(item => item.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Bootstrap obeys its deadline and does not hammer the vault during retry backoff.
        /// </summary>
        [Fact]
        public async Task BootstrapTimeoutEnforcesBackoff()
        {
            using InfrastructureDbContext database = CreateVaultDatabase();
            var cache = new KeyVaultConfigurationCache();
            var clock = new TestClock();
            var options = new TenantRuntimeOptions { ConfigurationLoadTimeout = TimeSpan.FromMilliseconds(50) };
            var pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            int reads = 0;
            Mock<SecretClient> core = CreateSecretClient("Test--Core", () =>
                Interlocked.Increment(ref reads) == 1 ? pending.Task : Task.FromResult("core-value"));
            KeyVaultConfigurationManager manager = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, _ => core.Object, options, clock);
            try
            {
                await manager.LoadTenantConfigurationAsync().WaitAsync(TimeSpan.FromSeconds(2));
                Assert.Equal("ConfigurationBootstrapTimedOut", manager.GetLastLoadDiagnostics().Stage);
                Assert.False(cache.GetStatus(1).BootstrapReady);
                Assert.False(await manager.LoadTenantConfigurationAsync());
                Assert.Equal(1, reads);

                clock.Advance(options.InitialRetryDelay);
                Assert.True(await manager.LoadTenantConfigurationAsync());
                Assert.Equal(2, reads);
            }
            finally
            {
                pending.TrySetResult("expired-value");
            }
        }

        /// <summary>
        /// Concurrent managers perform only one fetch for a tenant.
        /// </summary>
        [Fact]
        public async Task ConcurrentTenantSecretLoadsFetchOnce()
        {
            using InfrastructureDbContext database = CreateVaultDatabase();
            var cache = new KeyVaultConfigurationCache();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<SecretClient> core = CreateSecretClient("Test--Core", () => Task.FromResult("core-value"));
            Mock<SecretClient> tenant = CreateSecretClient("Test--Setting", () =>
            {
                entered.TrySetResult();
                return release.Task;
            });
            SecretClient Factory(Uri uri) => uri.Host == "doar-sas-core.vault.azure.net" ? core.Object : tenant.Object;
            KeyVaultConfigurationManager first = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, Factory);
            KeyVaultConfigurationManager second = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, Factory);
            Assert.True(first.LoadTenantConfiguration(), first.GetLastLoadDiagnostics()?.ExceptionMessage);
            Assert.True(second.LoadTenantConfiguration(), second.GetLastLoadDiagnostics()?.ExceptionMessage);
            Task<bool> firstLoad = first.EnsureTenantConfigurationLoaded(1, TenantDevelopmentOptions.ProductionOptions);
            Task<bool> secondLoad;
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                secondLoad = second.EnsureTenantConfigurationLoaded(1, TenantDevelopmentOptions.ProductionOptions);
                Assert.False(secondLoad.IsCompleted);
                tenant.Verify(item => item.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                release.TrySetResult("tenant-value");
            }

            Assert.True(await firstLoad.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.False(await secondLoad.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Same(first.GetTenantConfiguration(1), second.GetTenantConfiguration(1));
            core.Verify(item => item.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Concurrent transient managers share bootstrap rather than locking separate instances.
        /// </summary>
        [Fact]
        public async Task ConcurrentManagersBootstrapOnce()
        {
            using InfrastructureDbContext database = CreateVaultDatabase();
            var cache = new KeyVaultConfigurationCache();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<SecretClient> core = CreateSecretClient("Test--Core", () =>
            {
                entered.TrySetResult();
                return release.Task;
            });
            Mock<SecretClient> tenant = CreateSecretClient("Test--Setting", () => Task.FromResult("tenant-value"));
            SecretClient Factory(Uri uri) => uri.Host == "doar-sas-core.vault.azure.net" ? core.Object : tenant.Object;
            KeyVaultConfigurationManager first = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, Factory);
            KeyVaultConfigurationManager second = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, Factory);
            Task<bool> firstLoad = first.LoadTenantConfigurationAsync();
            Task<bool> secondLoad;
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                secondLoad = second.LoadTenantConfigurationAsync();
                Assert.False(secondLoad.IsCompleted);
                core.Verify(item => item.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                release.TrySetResult("core-value");
            }

            Assert.True(await firstLoad.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.True(await secondLoad.WaitAsync(TimeSpan.FromSeconds(5)));
            core.Verify(item => item.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// A fetch begun before reload cannot overwrite the replacement configuration.
        /// </summary>
        [Fact]
        public async Task ReloadDuringSecretLoadDoesNotRestoreStaleValues()
        {
            using InfrastructureDbContext database = CreateVaultDatabase();
            var cache = new KeyVaultConfigurationCache();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<SecretClient> core = CreateSecretClient("Test--Core", () => Task.FromResult("core-value"));
            int reads = 0;
            Mock<SecretClient> tenant = CreateSecretClient("Test--Setting", () =>
            {
                if (Interlocked.Increment(ref reads) == 1)
                {
                    entered.TrySetResult();
                    return release.Task;
                }

                return Task.FromResult("new-value");
            });
            SecretClient Factory(Uri uri) => uri.Host == "doar-sas-core.vault.azure.net" ? core.Object : tenant.Object;
            KeyVaultConfigurationManager manager = this.CreateManager(new ConfigurationBuilder().Build(), cache, database, Factory);
            Assert.True(manager.LoadTenantConfiguration(), manager.GetLastLoadDiagnostics()?.ExceptionMessage);
            Task<bool> oldLoad = manager.EnsureTenantConfigurationLoaded(1, TenantDevelopmentOptions.ProductionOptions);
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                cache.Clear();
                Assert.True(manager.LoadTenantConfiguration(), manager.GetLastLoadDiagnostics()?.ExceptionMessage);
                Assert.True(await manager.EnsureTenantConfigurationLoaded(1, TenantDevelopmentOptions.ProductionOptions));
            }
            finally
            {
                release.TrySetResult("old-value");
            }

            await oldLoad.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("new-value", manager.GetTenantConfiguration(1)["Test:Setting"]);
            core.Verify(item => item.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        /// <summary>
        /// Tenant overrides are consistent across indexed reads, sections, and binding.
        /// </summary>
        [Fact]
        public void TenantConfigurationSupportsSectionsAndBinding()
        {
            IConfiguration host = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Feature:Mode", "host" },
                { "Feature:Shared", "shared" },
                { "LocalEmailCapture:Enabled", "true" },
            }).Build();
            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var configuration = new TenantConfigurationRoot(host, accessor);
            Assert.Equal("host", configuration["Feature:Mode"]);
            SetTenantConfiguration(accessor.HttpContext, 1, new Dictionary<string, string>
            {
                { "Feature:Mode", "tenant" },
                { "Feature:Added", "added" },
                { "LocalEmailCapture:Enabled", "false" },
            });

            Assert.Equal("tenant", configuration["Feature:Mode"]);
            Assert.Equal("tenant", configuration.GetSection("Feature").GetValue<string>("Mode"));
            Dictionary<string, string> bound = configuration.GetSection("Feature").Get<Dictionary<string, string>>();
            Assert.Equal("tenant", bound["Mode"]);
            Assert.Equal("shared", bound["Shared"]);
            Assert.Equal("added", bound["Added"]);
            Assert.True(configuration.HostConfiguration.GetSection("LocalEmailCapture").GetValue<bool>("Enabled"));
            Assert.False(configuration.GetSection("LocalEmailCapture").GetValue<bool>("Enabled"));
        }

        /// <summary>
        /// Reusing the configuration adapter cannot retain another request's tenant values.
        /// </summary>
        [Fact]
        public void TenantConfigurationDoesNotLeakAcrossRequests()
        {
            IConfiguration host = new ConfigurationBuilder().Build();
            var accessor = new HttpContextAccessor();
            var configuration = new TenantConfigurationRoot(host, accessor);
            var first = new DefaultHttpContext();
            var second = new DefaultHttpContext();
            SetTenantConfiguration(first, 1, new Dictionary<string, string> { { "Test:Setting", "first" } });
            SetTenantConfiguration(second, 2, new Dictionary<string, string> { { "Test:Setting", "second" } });

            accessor.HttpContext = first;
            Assert.Equal("first", configuration["Test:Setting"]);
            accessor.HttpContext = second;
            Assert.Equal("second", configuration.GetSection("Test")["Setting"]);
            accessor.HttpContext = first;
            Assert.Equal("first", configuration["Test:Setting"]);
        }

        /// <summary>
        /// Named OAuth options are isolated regardless of which tenant is requested first.
        /// </summary>
        /// <param name="firstTenantId">First tenant.</param>
        /// <param name="secondTenantId">Second tenant.</param>
        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void AuthenticationOptionsAreIsolatedByTenant(int firstTenantId, int secondTenantId)
        {
            var accessor = new HttpContextAccessor();
            using ServiceProvider services = CreateAuthenticationServices(accessor);
            IOptionsMonitor<GoogleOptions> monitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
            Assert.Equal("host-client", monitor.Get("Google").ClientId);

            HttpContext firstContext = CreateAuthenticationContext(firstTenantId, $"tenant-{firstTenantId}");
            accessor.HttpContext = firstContext;
            GoogleOptions first = monitor.Get("Google");
            accessor.HttpContext = CreateAuthenticationContext(secondTenantId, $"tenant-{secondTenantId}");
            GoogleOptions second = monitor.Get("Google");

            Assert.Equal($"tenant-{firstTenantId}", first.ClientId);
            Assert.Equal($"tenant-{secondTenantId}", second.ClientId);
            Assert.NotSame(first, second);
            accessor.HttpContext = firstContext;
            Assert.Same(first, monitor.Get("Google"));
        }

        /// <summary>
        /// A replacement secret snapshot cannot reuse authentication options from the old snapshot.
        /// </summary>
        [Fact]
        public void AuthenticationOptionsFollowReplacementSecrets()
        {
            var accessor = new HttpContextAccessor();
            using ServiceProvider services = CreateAuthenticationServices(accessor);
            IOptionsMonitor<GoogleOptions> monitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
            accessor.HttpContext = CreateAuthenticationContext(1, "old-client");
            GoogleOptions oldOptions = monitor.Get("Google");

            accessor.HttpContext = CreateAuthenticationContext(1, "new-client");

            GoogleOptions newOptions = monitor.Get("Google");
            Assert.Equal("new-client", newOptions.ClientId);
            Assert.NotSame(oldOptions, newOptions);
            Assert.Same(newOptions, monitor.Get("Google"));
        }

        /// <summary>
        /// Background invalidation removes the named options for every tenant.
        /// </summary>
        [Fact]
        public void AuthenticationOptionsCanBeInvalidatedForAllTenants()
        {
            var accessor = new HttpContextAccessor();
            using ServiceProvider services = CreateAuthenticationServices(accessor);
            IOptionsMonitor<GoogleOptions> monitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
            accessor.HttpContext = CreateAuthenticationContext(1, "first");
            GoogleOptions first = monitor.Get("Google");
            accessor.HttpContext = CreateAuthenticationContext(2, "second");
            GoogleOptions second = monitor.Get("Google");

            accessor.HttpContext = null;
            Assert.True(services.GetRequiredService<IOptionsMonitorCache<GoogleOptions>>().TryRemove("Google"));
            accessor.HttpContext = CreateAuthenticationContext(1, "changed-first");
            Assert.Equal("changed-first", monitor.Get("Google").ClientId);
            Assert.NotSame(first, monitor.Get("Google"));
            accessor.HttpContext = CreateAuthenticationContext(2, "changed-second");
            Assert.Equal("changed-second", monitor.Get("Google").ClientId);
            Assert.NotSame(second, monitor.Get("Google"));
        }

        /// <summary>
        /// OAuth callback state is protected for the originating tenant only.
        /// </summary>
        [Fact]
        public void AuthenticationStateCannotBeUsedByAnotherTenant()
        {
            var accessor = new HttpContextAccessor();
            using ServiceProvider services = CreateAuthenticationServices(accessor);
            IOptionsMonitor<GoogleOptions> monitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
            accessor.HttpContext = CreateAuthenticationContext(1, "first");
            GoogleOptions first = monitor.Get("Google");
            string state = first.StateDataFormat.Protect(new AuthenticationProperties { RedirectUri = "/done" });
            accessor.HttpContext = CreateAuthenticationContext(2, "second");
            GoogleOptions second = monitor.Get("Google");

            Assert.Null(second.StateDataFormat.Unprotect(state));
            Assert.Equal("/done", first.StateDataFormat.Unprotect(state).RedirectUri);
        }

        /// <summary>
        /// A reload after the middleware accepts a tenant cannot swap the request's configuration or auth options.
        /// </summary>
        [Fact]
        public void PinnedTenantConfigurationSurvivesReloadForTheRequest()
        {
            var accessor = new HttpContextAccessor();
            using ServiceProvider services = CreateAuthenticationServices(accessor);
            IOptionsMonitor<GoogleOptions> monitor = services.GetRequiredService<IOptionsMonitor<GoogleOptions>>();
            var configuration = new TenantConfigurationRoot(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", "host-database" },
            }).Build(), accessor);
            HttpContext context = CreateAuthenticationContext(1, "pinned-client");
            var live = new Mock<IKeyVaultConfigurationManager>();
            context.Items[typeof(IKeyVaultConfigurationManager).Name] = live.Object;
            var secrets = new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", "tenant-database" },
                { "Authentication:Google:ClientId", "pinned-client" },
                { "Authentication:Google:ClientSecret", "test-only" },
            };
            context.CaptureTenantSpecificConfiguration(1, secrets);
            accessor.HttpContext = context;
            Assert.Equal("tenant-database", configuration.GetConnectionString("DefaultConnection"));

            live.Setup(item => item.GetTenantConfiguration(1)).Returns((Dictionary<string, string>)null);
            Assert.Equal("tenant-database", configuration.GetConnectionString("DefaultConnection"));
            Assert.Equal("pinned-client", monitor.Get("Google").ClientId);

            live.Setup(item => item.GetTenantConfiguration(1)).Returns(new Dictionary<string, string>
            {
                { "ConnectionStrings:DefaultConnection", "replacement-database" },
                { "Authentication:Google:ClientId", "replacement-client" },
            });
            Assert.Equal("tenant-database", configuration.GetConnectionString("DefaultConnection"));
            Assert.Equal("pinned-client", monitor.Get("Google").ClientId);
        }

        private static ServiceProvider CreateAuthenticationServices(IHttpContextAccessor accessor)
        {
            IConfiguration host = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Authentication:Google:ClientId", "host-client" },
                { "Authentication:Google:ClientSecret", "test-only" },
            }).Build();
            var configuration = new TenantConfigurationRoot(host, accessor);
            var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider().CreateProtector("test-state"));
            return new ServiceCollection()
                .AddSingleton(accessor)
                .AddOptions()
                .Configure<GoogleOptions>("Google", options => options.StateDataFormat = stateFormat)
                .AddSingleton<IPostConfigureOptions<GoogleOptions>>(new GooglePostConfigureOptions(configuration))
                .AddSingleton<IOptionsMonitorCache<GoogleOptions>, TenantAuthenticationOptionsCache<GoogleOptions>>()
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        }

        private static HttpContext CreateAuthenticationContext(int tenantId, string clientId)
        {
            var context = new DefaultHttpContext();
            SetTenantConfiguration(context, tenantId, new Dictionary<string, string>
            {
                { "Authentication:Google:ClientId", clientId },
                { "Authentication:Google:ClientSecret", "test-only" },
            });
            return context;
        }

        private static void SetTenantConfiguration(HttpContext context, int tenantId, Dictionary<string, string> secrets)
        {
            context.SetTenant(new Model.Tenant { Id = tenantId });
            var manager = new Mock<IKeyVaultConfigurationManager>();
            manager.Setup(item => item.GetTenantConfiguration(tenantId)).Returns(secrets);
            context.Items[typeof(IKeyVaultConfigurationManager).Name] = manager.Object;
        }

        private static InfrastructureDbContext CreateVaultDatabase()
        {
            var database = new InfrastructureDbContext(new DbContextOptionsBuilder<InfrastructureDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            for (int id = 1; id <= 2; id++)
            {
                database.Tenants.Add(new Model.Tenant
                {
                    Id = id,
                    Name = id == 1 ? "first" : "second",
                    PublicId = Guid.NewGuid(),
                    Created = DateTime.UtcNow,
                    KeyVaultConfigurations = new List<KeyVaultConfiguration>
                    {
                        new KeyVaultConfiguration
                        {
                            Vault = new Uri(id == 1 ? "https://first.vault.azure.net/" : "https://second.vault.azure.net/"),
                            Environment = "localhost",
                            Created = DateTime.UtcNow,
                        },
                    },
                });
            }

            database.SaveChanges();
            return database;
        }

        private static Mock<SecretClient> CreateSecretClient(string name, Func<Task<string>> readValue)
        {
            var client = new Mock<SecretClient>(MockBehavior.Strict);
            client.Setup(item => item.GetPropertiesOfSecretsAsync(It.IsAny<CancellationToken>()))
                .Returns(() => AsyncPageable<SecretProperties>.FromPages(new[]
                {
                    Page<SecretProperties>.FromValues(new[] { new SecretProperties(name) }, null, Mock.Of<Response>()),
                }));
            client.Setup(item => item.GetSecretAsync(name, It.IsAny<string>(), It.IsAny<SecretContentType?>(), It.IsAny<CancellationToken>()))
                .Returns(async () => Response.FromValue(new KeyVaultSecret(name, await readValue()), Mock.Of<Response>()));
            return client;
        }

        private KeyVaultConfigurationManager CreateManager(
            IConfiguration configuration,
            KeyVaultConfigurationCache cache,
            InfrastructureDbContext database = null,
            Func<Uri, SecretClient> createClient = null,
            TenantRuntimeOptions runtimeOptions = null,
            TimeProvider clock = null)
        {
            return new TestConfigurationManager(
                database ?? this.fixture.ServiceProvider.GetRequiredService<InfrastructureDbContext>(),
                this.fixture.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                this.fixture.ServiceProvider.GetRequiredService<TelemetryClient>(),
                cache,
                configuration,
                createClient,
                runtimeOptions,
                clock);
        }

        private sealed class TestConfigurationManager : KeyVaultConfigurationManager
        {
            private readonly Func<Uri, SecretClient> createClient;

            public TestConfigurationManager(
                InfrastructureDbContext database,
                IWebHostEnvironment environment,
                TelemetryClient telemetry,
                KeyVaultConfigurationCache cache,
                IConfiguration configuration,
                Func<Uri, SecretClient> createClient,
                TenantRuntimeOptions runtimeOptions,
                TimeProvider clock)
                : base(database, environment, telemetry, cache, configuration, NullLogger<KeyVaultConfigurationManager>.Instance, runtimeOptions == null ? null : Options.Create(runtimeOptions), clock)
            {
                this.createClient = createClient;
            }

            protected override SecretClient CreateSecretClient(Uri vaultUri, TokenCredential credential)
            {
                return this.createClient == null ? base.CreateSecretClient(vaultUri, credential) : this.createClient(vaultUri);
            }
        }

        private sealed class TestClock : TimeProvider
        {
            private DateTimeOffset now = DateTimeOffset.UtcNow;

            public override DateTimeOffset GetUtcNow() => this.now;

            public void Advance(TimeSpan elapsed) => this.now += elapsed;
        }
    }
}
