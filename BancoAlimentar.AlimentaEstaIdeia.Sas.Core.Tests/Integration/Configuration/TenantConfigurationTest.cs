// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.Integration.Configuration
{
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
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using System.Collections.Generic;
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
            using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            KeyVaultConfigurationManager manager = this.CreateManager(configuration, memoryCache);
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
            using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            KeyVaultConfigurationManager manager = this.CreateManager(new ConfigurationBuilder().Build(), memoryCache);

            Assert.False(await manager.EnsureTenantConfigurationLoaded(tenantId, TenantDevelopmentOptions.ProductionOptions));
            Assert.Null(manager.GetTenantConfiguration(tenantId));
        }

        /// <summary>
        /// The Key Vault path reuses memory-cached secrets without requesting initialization.
        /// </summary>
        [Fact]
        public async Task MemoryCachedSecretsPreserveLoadResult()
        {
            int tenantId = Interlocked.Decrement(ref nextTenantId);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { { "Test:Setting", "local" } })
                .Build();
            using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            var secrets = new Dictionary<string, string> { { "Test:Setting", "cached" } };
            memoryCache.Set($"TenantSecret-{tenantId}", secrets);
            KeyVaultConfigurationManager manager = this.CreateManager(configuration, memoryCache);

            Assert.False(await manager.EnsureTenantConfigurationLoaded(tenantId, new TenantDevelopmentOptions { UseSecrets = false }));
            Assert.Same(secrets, manager.GetTenantConfiguration(tenantId));
            Assert.Equal("cached", secrets["Test:Setting"]);
        }

        /// <summary>
        /// Local overrides update existing memory-cached keys and preserve unrelated secrets.
        /// </summary>
        [Fact]
        public async Task LocalConfigurationMergesIntoMemoryCachedSecrets()
        {
            int tenantId = Interlocked.Decrement(ref nextTenantId);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Test:Setting", "local" },
                    { "Test:Added", "added" },
                })
                .Build();
            using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
            var secrets = new Dictionary<string, string>
            {
                { "Test:Setting", "cached" },
                { "Test:Retained", "retained" },
            };
            memoryCache.Set($"TenantSecret-{tenantId}", secrets);
            KeyVaultConfigurationManager manager = this.CreateManager(configuration, memoryCache);

            Assert.True(await manager.EnsureTenantConfigurationLoaded(tenantId, new TenantDevelopmentOptions { UseSecrets = true }));
            Assert.Same(secrets, manager.GetTenantConfiguration(tenantId));
            Assert.Equal("local", secrets["Test:Setting"]);
            Assert.Equal("added", secrets["Test:Added"]);
            Assert.Equal("retained", secrets["Test:Retained"]);
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

            accessor.HttpContext = CreateAuthenticationContext(firstTenantId, $"tenant-{firstTenantId}");
            GoogleOptions first = monitor.Get("Google");
            accessor.HttpContext = CreateAuthenticationContext(secondTenantId, $"tenant-{secondTenantId}");
            GoogleOptions second = monitor.Get("Google");

            Assert.Equal($"tenant-{firstTenantId}", first.ClientId);
            Assert.Equal($"tenant-{secondTenantId}", second.ClientId);
            Assert.NotSame(first, second);
            accessor.HttpContext = CreateAuthenticationContext(firstTenantId, $"tenant-{firstTenantId}");
            Assert.Same(first, monitor.Get("Google"));
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

        private KeyVaultConfigurationManager CreateManager(IConfiguration configuration, IMemoryCache memoryCache)
        {
            return new KeyVaultConfigurationManager(
                this.fixture.ServiceProvider.GetRequiredService<InfrastructureDbContext>(),
                this.fixture.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                this.fixture.ServiceProvider.GetRequiredService<TelemetryClient>(),
                memoryCache,
                configuration,
                NullLogger<KeyVaultConfigurationManager>.Instance);
        }
    }
}
