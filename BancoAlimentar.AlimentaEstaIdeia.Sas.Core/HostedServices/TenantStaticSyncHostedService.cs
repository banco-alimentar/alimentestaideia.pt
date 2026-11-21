// -----------------------------------------------------------------------
// <copyright file="TenantStaticSyncHostedService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.HostedServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Synchronize the tenant static files to the AppService local hard drive.
    /// </summary>
    public class TenantStaticSyncHostedService : BackgroundService
    {
        private readonly ILogger<TenantStaticSyncHostedService> logger;
        private readonly IServiceProvider services;
        private readonly TenantRuntimeOptions options;
        private readonly TimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantStaticSyncHostedService"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="services">Service provider.</param>
        /// <param name="runtimeOptions">Refresh and retry controls.</param>
        /// <param name="timeProvider">Runtime clock.</param>
        public TenantStaticSyncHostedService(
            ILogger<TenantStaticSyncHostedService> logger,
            IServiceProvider services,
            IOptions<TenantRuntimeOptions>? runtimeOptions = null,
            TimeProvider? timeProvider = null)
        {
            this.logger = logger;
            this.services = services;
            this.options = runtimeOptions?.Value ?? new TenantRuntimeOptions();
            this.timeProvider = timeProvider ?? TimeProvider.System;
            if (!this.options.IsValid())
            {
                throw new ArgumentException("Static refresh intervals must be positive and retry delays must be bounded.", nameof(runtimeOptions));
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            KeyVaultConfigurationCache configurationCache = this.services.GetRequiredService<KeyVaultConfigurationCache>();
            ITenantStaticLocalCacheService statusCache = this.services.GetRequiredService<ITenantStaticLocalCacheService>();
            var schedules = new Dictionary<Guid, RefreshSchedule>();
            Guid generation = configurationCache.Generation;
            DateTimeOffset nextPassAt = this.timeProvider.GetUtcNow();
            int bootstrapFailures = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                CancellationToken reloadToken = configurationCache.ReloadToken;
                if (generation != configurationCache.Generation)
                {
                    generation = configurationCache.Generation;
                    nextPassAt = this.timeProvider.GetUtcNow();
                    bootstrapFailures = 0;
                    foreach (var schedule in schedules)
                    {
                        schedule.Value.NextAttemptAt = nextPassAt;
                        schedule.Value.Failures = 0;
                        statusCache.SetNextAttempt(schedule.Key, nextPassAt);
                    }
                }

                using var passCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, reloadToken);
                CancellationToken passToken = passCancellation.Token;
                try
                {
                    TimeSpan delay = nextPassAt - this.timeProvider.GetUtcNow();
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, this.timeProvider, passToken);
                    }

                    using IServiceScope scope = this.services.CreateScope();
                    IWebHostEnvironment environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                    IKeyVaultConfigurationManager configurationManager = scope.ServiceProvider.GetRequiredService<IKeyVaultConfigurationManager>();
                    ITenantStaticLocalCacheService localCache = scope.ServiceProvider.GetRequiredService<ITenantStaticLocalCacheService>();
                    if (!await configurationManager.LoadTenantConfigurationAsync(passToken))
                    {
                        throw new InvalidOperationException("Tenant configuration bootstrap is unavailable.");
                    }

                    InfrastructureDbContext context = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
                    List<Model.Tenant> tenants = await context.Tenants.AsNoTracking()
                        .Where(tenant => tenant.Domains.Any(domain => domain.Environment == environment.EnvironmentName))
                        .ToListAsync(passToken);
                    bootstrapFailures = 0;

                    foreach (Model.Tenant tenant in tenants)
                    {
                        passToken.ThrowIfCancellationRequested();
                        if (!schedules.TryGetValue(tenant.PublicId, out RefreshSchedule? schedule))
                        {
                            schedule = new RefreshSchedule(this.timeProvider.GetUtcNow());
                            schedules.Add(tenant.PublicId, schedule);
                        }

                        if (schedule.NextAttemptAt > this.timeProvider.GetUtcNow())
                        {
                            continue;
                        }

                        try
                        {
                            await configurationManager.EnsureTenantConfigurationLoaded(tenant.Id, TenantDevelopmentOptions.ProductionOptions, passToken);
                            string? connectionString = configurationManager.GetTenantConfiguration(tenant.Id)?["AzureStorage:ConnectionString"];
                            if (string.IsNullOrWhiteSpace(connectionString))
                            {
                                throw new InvalidOperationException("Tenant storage configuration is unavailable.");
                            }

                            await localCache.ResyncFromBlobAsync(tenant.PublicId, tenant.NormalizedName, connectionString, onlyIfSizeChanged: true, passToken);
                            schedule.NextAttemptAt = this.timeProvider.GetUtcNow() + this.options.StaticAssetRefreshInterval;
                            schedule.Failures = 0;
                        }
                        catch (OperationCanceledException) when (passToken.IsCancellationRequested)
                        {
                            localCache.SetNextAttempt(tenant.PublicId, null);
                            throw;
                        }
                        catch (Exception exception)
                        {
                            localCache.RecordFailure(tenant.PublicId, exception);
                            schedule.Failures = Math.Min(schedule.Failures + 1, 31);
                            schedule.NextAttemptAt = this.timeProvider.GetUtcNow() + this.options.GetRetryDelay(schedule.Failures);
                            this.logger.LogWarning("Static refresh failed for tenant {TenantId}: {FailureType}.", tenant.Id, localCache.GetStatus(tenant.PublicId).LastFailureType);
                        }

                        localCache.SetNextAttempt(tenant.PublicId, schedule.NextAttemptAt);
                    }

                    var currentTenants = tenants.Select(tenant => tenant.PublicId).ToHashSet();
                    foreach (Guid removedTenant in schedules.Keys.Where(id => !currentTenants.Contains(id)).ToArray())
                    {
                        schedules.Remove(removedTenant);
                        localCache.SetNextAttempt(removedTenant, null);
                    }

                    nextPassAt = schedules.Count == 0
                        ? this.timeProvider.GetUtcNow() + this.options.StaticAssetRefreshInterval
                        : schedules.Values.Min(schedule => schedule.NextAttemptAt);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (OperationCanceledException) when (reloadToken.IsCancellationRequested)
                {
                    // The next iteration uses the replacement generation immediately.
                }
                catch (Exception exception)
                {
                    bootstrapFailures = Math.Min(bootstrapFailures + 1, 31);
                    nextPassAt = this.timeProvider.GetUtcNow() + this.options.GetRetryDelay(bootstrapFailures);
                    foreach (var schedule in schedules.Where(schedule => schedule.Value.NextAttemptAt <= this.timeProvider.GetUtcNow()))
                    {
                        statusCache.RecordFailure(schedule.Key, exception);
                        schedule.Value.NextAttemptAt = nextPassAt;
                        statusCache.SetNextAttempt(schedule.Key, nextPassAt);
                    }

                    this.logger.LogWarning("Static refresh bootstrap failed: {FailureType}. Retrying.", exception.GetType().Name);
                }
            }

            foreach (Guid tenantId in schedules.Keys)
            {
                statusCache.SetNextAttempt(tenantId, null);
            }
        }

        private sealed class RefreshSchedule
        {
            public RefreshSchedule(DateTimeOffset nextAttemptAt)
            {
                this.NextAttemptAt = nextAttemptAt;
            }

            public DateTimeOffset NextAttemptAt { get; set; }

            public int Failures { get; set; }
        }
    }
}
