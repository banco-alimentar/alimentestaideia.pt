// -----------------------------------------------------------------------
// <copyright file="MultiTenantFunctionConfigurationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Tests tenant configuration loading for multi-tenant functions.
    /// </summary>
    [Collection(WebsiteSlotNameCollection.Name)]
    public class MultiTenantFunctionConfigurationTests
    {
        /// <summary>
        /// A background invalidation between tenants is re-bootstrapped instead of running with empty configuration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task TenantAfterInvalidationUsesRebootstrappedConfiguration()
        {
            var cache = new KeyVaultConfigurationCache();
            var manager = new GenerationAwareManager(cache);
            manager.Secrets[1] = CreateSecrets("first");
            manager.Secrets[2] = CreateSecrets("second");
            using ServiceProvider provider = CreateProvider(cache, manager);
            var function = new MultiTenantFunction(TelemetryConfiguration.CreateDefault(), provider);
            var executed = new Dictionary<string, string>();
            function.ExecuteFunction = (unitOfWork, context) =>
            {
                executed.Add(function.Configuration["Tenant:Name"], function.Configuration["Test:Setting"]);
                cache.Clear();
                return Task.CompletedTask;
            };

            await function.RunFunctionCore();

            Assert.Equal("first", executed["first"]);
            Assert.Equal("second", executed["second"]);
            Assert.Equal(2, manager.Bootstraps);
        }

        /// <summary>
        /// A tenant whose configuration cannot be loaded is not executed with missing configuration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task TenantWithoutConfigurationIsNotExecuted()
        {
            var cache = new KeyVaultConfigurationCache();
            var manager = new GenerationAwareManager(cache);
            manager.Secrets[2] = CreateSecrets("second");
            using ServiceProvider provider = CreateProvider(cache, manager);
            var function = new MultiTenantFunction(TelemetryConfiguration.CreateDefault(), provider);
            var executed = new List<string>();
            function.ExecuteFunction = (unitOfWork, context) =>
            {
                executed.Add(function.Configuration["Tenant:Name"]);
                return Task.CompletedTask;
            };

            await function.RunFunctionCore();

            Assert.Equal(new[] { "second" }, executed);
        }

        private static Dictionary<string, string> CreateSecrets(string value)
        {
            return new Dictionary<string, string>
            {
                { "Test:Setting", value },
                { "ConnectionStrings:DefaultConnection", "Server=unused;Database=unused" },
            };
        }

        private static ServiceProvider CreateProvider(KeyVaultConfigurationCache cache, IKeyVaultConfigurationManager manager)
        {
            string database = Guid.NewGuid().ToString();
            ServiceProvider provider = new ServiceCollection()
                .AddSingleton(cache)
                .AddSingleton(manager)
                .AddDbContext<InfrastructureDbContext>(options => options.UseInMemoryDatabase(database))
                .BuildServiceProvider();
            InfrastructureDbContext context = provider.GetRequiredService<InfrastructureDbContext>();
            context.Tenants.Add(new Tenant { Id = 1, Name = "first", PublicId = Guid.NewGuid(), Created = DateTime.UtcNow });
            context.Tenants.Add(new Tenant { Id = 2, Name = "second", PublicId = Guid.NewGuid(), Created = DateTime.UtcNow });
            context.SaveChanges();
            return provider;
        }

        /// <summary>
        /// Mirrors the real manager: tenant loads only succeed against a bootstrapped cache generation.
        /// </summary>
        private sealed class GenerationAwareManager : IKeyVaultConfigurationManager
        {
            private readonly KeyVaultConfigurationCache cache;
            private readonly HashSet<Guid> bootstrapped = new HashSet<Guid>();
            private readonly Dictionary<(Guid Generation, int TenantId), Dictionary<string, string>> loaded = new();

            public GenerationAwareManager(KeyVaultConfigurationCache cache)
            {
                this.cache = cache;
            }

            public Dictionary<int, Dictionary<string, string>> Secrets { get; } = new Dictionary<int, Dictionary<string, string>>();

            public int Bootstraps { get; private set; }

            public Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions)
            {
                return this.EnsureTenantConfigurationLoaded(tenantId, developmentOptions, CancellationToken.None);
            }

            public Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions, CancellationToken cancellationToken)
            {
                Guid generation = this.cache.Generation;
                if (!this.bootstrapped.Contains(generation) || !this.Secrets.TryGetValue(tenantId, out Dictionary<string, string> secrets))
                {
                    return Task.FromResult(false);
                }

                this.loaded[(generation, tenantId)] = secrets;
                return Task.FromResult(true);
            }

            public Dictionary<string, string> GetTenantConfiguration(int tenantId)
            {
                return this.loaded.TryGetValue((this.cache.Generation, tenantId), out Dictionary<string, string> secrets) ? secrets : null;
            }

            public bool LoadTenantConfiguration()
            {
                return this.LoadTenantConfigurationAsync().GetAwaiter().GetResult();
            }

            public Task<bool> LoadTenantConfigurationAsync(CancellationToken cancellationToken = default)
            {
                if (this.bootstrapped.Add(this.cache.Generation))
                {
                    this.Bootstraps++;
                }

                return Task.FromResult(true);
            }

            public KeyVaultConfigurationLoadDiagnostics GetLastLoadDiagnostics()
            {
                return null;
            }
        }
    }
}
