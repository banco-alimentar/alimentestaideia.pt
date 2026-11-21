// -----------------------------------------------------------------------
// <copyright file="RuntimeCacheInvalidationHostedService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>Observes reload signals across instances of the same environment.</summary>
    public sealed class RuntimeCacheInvalidationHostedService : BackgroundService
    {
        private readonly RuntimeCacheInvalidationService invalidation;
        private readonly TenantRuntimeOptions options;
        private readonly TimeProvider clock;
        private readonly ILogger<RuntimeCacheInvalidationHostedService> logger;

        /// <summary>Initializes revision polling.</summary>
        /// <param name="invalidation">Revision coordinator.</param>
        /// <param name="options">Host-level intervals.</param>
        /// <param name="clock">Polling clock.</param>
        /// <param name="logger">Failure logger.</param>
        public RuntimeCacheInvalidationHostedService(
            RuntimeCacheInvalidationService invalidation,
            IOptions<TenantRuntimeOptions> options,
            TimeProvider clock,
            ILogger<RuntimeCacheInvalidationHostedService> logger)
        {
            this.invalidation = invalidation;
            this.options = options.Value;
            this.clock = clock;
            this.logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int failures = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delay = this.options.InvalidationPollInterval;
                using var deadline = new CancellationTokenSource(this.options.ConfigurationLoadTimeout, this.clock);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, deadline.Token);
                try
                {
                    await this.invalidation.PollAsync(linked.Token);
                    failures = 0;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    this.invalidation.RecordFailure(exception);
                    this.logger.LogError(exception, "Unable to observe shared tenant cache revisions");
                    failures = Math.Min(failures + 1, 31);
                    TimeSpan retryDelay = this.options.GetRetryDelay(failures);
                    delay = retryDelay < delay ? retryDelay : delay;
                }

                try
                {
                    await Task.Delay(delay, this.clock, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
