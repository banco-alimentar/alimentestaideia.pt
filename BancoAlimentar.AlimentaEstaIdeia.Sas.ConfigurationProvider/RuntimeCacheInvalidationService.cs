// -----------------------------------------------------------------------
// <copyright file="RuntimeCacheInvalidationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Publishes durable reload signals and applies them to the current application instance.
    /// </summary>
    public sealed class RuntimeCacheInvalidationService
    {
        private readonly object sync = new object();
        private readonly IServiceScopeFactory scopes;
        private readonly KeyVaultConfigurationCache vaultCache;
        private readonly InMemoryCacheService databaseCache;
        private readonly IMemoryCache memoryCache;
        private readonly IEnumerable<ITenantAuthenticationOptionsCache> authenticationCaches;
        private readonly IHostEnvironment environment;
        private readonly TimeProvider clock;
        private readonly ILogger<RuntimeCacheInvalidationService> logger;
        private RuntimeInvalidationStatus status = new RuntimeInvalidationStatus(null, null, null);

        /// <summary>Initializes shared cache invalidation for this host.</summary>
        /// <param name="scopes">Infrastructure database scopes.</param>
        /// <param name="vaultCache">Vault cache.</param>
        /// <param name="databaseCache">Database-backed tenant settings.</param>
        /// <param name="memoryCache">Lookup and application caches.</param>
        /// <param name="authenticationCaches">Authentication option caches.</param>
        /// <param name="environment">Host environment.</param>
        /// <param name="clock">Publication and observation clock.</param>
        /// <param name="logger">Publication failure logger.</param>
        public RuntimeCacheInvalidationService(
            IServiceScopeFactory scopes,
            KeyVaultConfigurationCache vaultCache,
            InMemoryCacheService databaseCache,
            IMemoryCache memoryCache,
            IEnumerable<ITenantAuthenticationOptionsCache> authenticationCaches,
            IHostEnvironment environment,
            TimeProvider clock,
            ILogger<RuntimeCacheInvalidationService> logger)
        {
            this.scopes = scopes;
            this.vaultCache = vaultCache;
            this.databaseCache = databaseCache;
            this.memoryCache = memoryCache;
            this.authenticationCaches = authenticationCaches;
            this.environment = environment;
            this.clock = clock;
            this.logger = logger;
        }

        /// <summary>Gets observation status without exposing database details.</summary>
        /// <returns>The current status.</returns>
        public RuntimeInvalidationStatus GetStatus()
        {
            lock (this.sync)
            {
                return this.status;
            }
        }

        /// <summary>Publishes a reload before clearing local caches.</summary>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>The durable revision.</returns>
        public async Task<Guid> PublishAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await this.PublishRevisionAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                this.RecordFailure(exception);
                this.logger.LogError(exception, "Unable to publish shared tenant cache invalidation");
                throw;
            }
        }

        private async Task<Guid> PublishRevisionAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = this.scopes.CreateAsyncScope();
            InfrastructureDbContext database = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            RuntimeCacheRevision? row = await database.RuntimeCacheRevisions
                .SingleOrDefaultAsync(item => item.Environment == this.environment.EnvironmentName, cancellationToken);
            bool creating = row == null;
            row ??= new RuntimeCacheRevision { Environment = this.environment.EnvironmentName };
            Guid revision = Guid.NewGuid();
            row.Revision = revision;
            row.PublishedAt = this.clock.GetUtcNow();
            if (creating)
            {
                database.RuntimeCacheRevisions.Add(row);
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (creating)
            {
                database.Entry(row).State = EntityState.Detached;
                RuntimeCacheRevision? concurrent = await database.RuntimeCacheRevisions
                    .SingleOrDefaultAsync(item => item.Environment == this.environment.EnvironmentName, cancellationToken);
                if (concurrent == null)
                {
                    throw;
                }

                this.logger.LogInformation("Retrying concurrent cache revision creation for {Environment}", this.environment.EnvironmentName);
                concurrent.Revision = revision;
                concurrent.PublishedAt = row.PublishedAt;
                await database.SaveChangesAsync(cancellationToken);
            }

            this.ApplyRevision(revision);
            return revision;
        }

        /// <summary>Observes durable revisions without writing to the database.</summary>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>A task representing the observation.</returns>
        public async Task PollAsync(CancellationToken cancellationToken = default)
        {
            await using AsyncServiceScope scope = this.scopes.CreateAsyncScope();
            InfrastructureDbContext database = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
            RuntimeCacheRevision? row = await database.RuntimeCacheRevisions.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Environment == this.environment.EnvironmentName, cancellationToken);
            if (row != null)
            {
                this.ApplyRevision(row.Revision);
            }

            lock (this.sync)
            {
                this.status = this.status with { LastSuccessfulPollAt = this.clock.GetUtcNow(), LastFailureType = null };
            }
        }

        /// <summary>Records a failed observation using only an exception category.</summary>
        /// <param name="exception">Observation failure.</param>
        public void RecordFailure(Exception exception)
        {
            lock (this.sync)
            {
                this.status = this.status with { LastFailureType = exception.GetType().Name };
            }
        }

        private void ApplyRevision(Guid revision)
        {
            lock (this.sync)
            {
                if (this.status.LastObservedRevision == revision)
                {
                    return;
                }

                this.vaultCache.Clear();
                this.databaseCache.Clear();
                foreach (ITenantAuthenticationOptionsCache authenticationCache in this.authenticationCaches)
                {
                    authenticationCache.Clear();
                }

                if (this.memoryCache is MemoryCache cache)
                {
                    cache.Compact(1.0);
                }

                this.status = this.status with { LastObservedRevision = revision, LastFailureType = null };
            }
        }
    }

    /// <summary>A secret-free snapshot of shared revision observation.</summary>
    public sealed record RuntimeInvalidationStatus(Guid? LastObservedRevision, DateTimeOffset? LastSuccessfulPollAt, string? LastFailureType);
}
