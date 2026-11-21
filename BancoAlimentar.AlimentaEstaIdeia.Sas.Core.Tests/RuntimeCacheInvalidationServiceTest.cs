// -----------------------------------------------------------------------
// <copyright file="RuntimeCacheInvalidationServiceTest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Moq;
    using Xunit;

    /// <summary>Tests durable invalidation between isolated application hosts.</summary>
    public class RuntimeCacheInvalidationServiceTest
    {
        /// <summary>Daily polling is valid, while nonpositive and longer intervals are rejected.</summary>
        /// <param name="hours">Polling interval in hours.</param>
        /// <param name="valid">Whether host validation should accept the interval.</param>
        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(24, true)]
        [InlineData(25, false)]
        public void PollingIntervalValidationAcceptsDailyBoundary(int hours, bool valid)
        {
            var options = new TenantRuntimeOptions { InvalidationPollInterval = TimeSpan.FromHours(hours) };

            Assert.Equal(valid, options.IsValid());
        }

        /// <summary>One revision clears all relevant caches in the same environment only.</summary>
        [Fact]
        public async Task RevisionInvalidatesPeerHostsWithoutCrossingEnvironments()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            using ServiceProvider first = CreateHost(connection, "Development");
            using ServiceProvider second = CreateHost(connection, "Development");
            using ServiceProvider production = CreateHost(connection, "Production");
            using IServiceScope schemaScope = first.CreateScope();
            schemaScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>().Database.EnsureCreated();
            await PopulateCaches(first);
            await PopulateCaches(second);
            await PopulateCaches(production);
            RuntimeCacheInvalidationService publisher = first.GetRequiredService<RuntimeCacheInvalidationService>();
            RuntimeCacheInvalidationService observer = second.GetRequiredService<RuntimeCacheInvalidationService>();

            Guid revision = await publisher.PublishAsync();
            await observer.PollAsync();
            await production.GetRequiredService<RuntimeCacheInvalidationService>().PollAsync();

            Assert.Null(first.GetRequiredService<IKeyVaultConfigurationManager>().GetTenantConfiguration(1));
            Assert.Null(second.GetRequiredService<IKeyVaultConfigurationManager>().GetTenantConfiguration(1));
            Assert.Equal("Production", production.GetRequiredService<IKeyVaultConfigurationManager>().GetTenantConfiguration(1)["Test:Setting"]);
            Assert.Null(second.GetRequiredService<InMemoryCacheService>().GetTenantConfiguration(1));
            Assert.False(second.GetRequiredService<IMemoryCache>().TryGetValue("runtime-test", out _));
            Assert.Equal(revision, observer.GetStatus().LastObservedRevision);
            Guid generation = second.GetRequiredService<KeyVaultConfigurationCache>().Generation;

            await observer.PollAsync();
            Assert.Equal(generation, second.GetRequiredService<KeyVaultConfigurationCache>().Generation);
            Mock.Get(second.GetRequiredService<ITenantAuthenticationOptionsCache>()).Verify(item => item.Clear(), Times.Once);
        }

        /// <summary>A failed publication is explicit and retains the last usable caches.</summary>
        [Fact]
        public async Task FailedPublicationDoesNotClearLocalConfiguration()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            using ServiceProvider host = CreateHost(connection, "Development");
            using IServiceScope schemaScope = host.CreateScope();
            schemaScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>().Database.EnsureCreated();
            await PopulateCaches(host);
            connection.Close();
            RuntimeCacheInvalidationService invalidation = host.GetRequiredService<RuntimeCacheInvalidationService>();

            await Assert.ThrowsAsync<SqliteException>(() => invalidation.PublishAsync());

            Assert.Equal("Development", host.GetRequiredService<IKeyVaultConfigurationManager>().GetTenantConfiguration(1)["Test:Setting"]);
            Assert.Null(invalidation.GetStatus().LastObservedRevision);
            Assert.Equal(nameof(SqliteException), invalidation.GetStatus().LastFailureType);
        }

        /// <summary>A database settings query that finishes after a reload cannot restore pre-reload values.</summary>
        [Fact]
        public async Task StaleDatabaseSettingsAreRejectedAfterInvalidation()
        {
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            using ServiceProvider host = CreateHost(connection, "Development");
            using IServiceScope schemaScope = host.CreateScope();
            schemaScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>().Database.EnsureCreated();
            InMemoryCacheService databaseCache = host.GetRequiredService<InMemoryCacheService>();
            long inFlightGeneration = databaseCache.Generation;

            await host.GetRequiredService<RuntimeCacheInvalidationService>().PublishAsync();

            Assert.False(databaseCache.SetTenantConfiguration(1, new Dictionary<string, string> { { "Test", "stale" } }, inFlightGeneration));
            Assert.Null(databaseCache.GetTenantConfiguration(1));
            Assert.True(databaseCache.SetTenantConfiguration(1, new Dictionary<string, string> { { "Test", "fresh" } }, databaseCache.Generation));
            Assert.Equal("fresh", databaseCache.GetTenantConfiguration(1)["Test"]);
        }

        private static ServiceProvider CreateHost(SqliteConnection connection, string environmentName)
        {
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.EnvironmentName).Returns(environmentName);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Test:Setting", environmentName },
            }).Build();
            return new ServiceCollection()
                .AddLogging()
                .AddMemoryCache()
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<IHostEnvironment>(environment.Object)
                .AddSingleton<IWebHostEnvironment>(environment.Object)
                .AddSingleton(TimeProvider.System)
                .AddSingleton(_ => new TelemetryConfiguration { DisableTelemetry = true })
                .AddSingleton(provider => new TelemetryClient(provider.GetRequiredService<TelemetryConfiguration>()))
                .AddSingleton<KeyVaultConfigurationCache>()
                .AddSingleton<InMemoryCacheService>()
                .AddSingleton(Mock.Of<ITenantAuthenticationOptionsCache>())
                .AddDbContext<InfrastructureDbContext>(options => options.UseSqlite(connection))
                .AddTransient<IKeyVaultConfigurationManager, KeyVaultConfigurationManager>()
                .AddSingleton<RuntimeCacheInvalidationService>()
                .BuildServiceProvider();
        }

        private static async Task PopulateCaches(ServiceProvider host)
        {
            await host.GetRequiredService<IKeyVaultConfigurationManager>()
                .EnsureTenantConfigurationLoaded(1, new TenantDevelopmentOptions { UseSecrets = true });
            host.GetRequiredService<InMemoryCacheService>().SetTenantConfiguration(1, new Dictionary<string, string> { { "Test", "cached" } });
            host.GetRequiredService<IMemoryCache>().Set("runtime-test", "cached");
        }
    }
}
