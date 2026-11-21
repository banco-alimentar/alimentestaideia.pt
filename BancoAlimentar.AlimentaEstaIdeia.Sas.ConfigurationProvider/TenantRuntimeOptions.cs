// -----------------------------------------------------------------------
// <copyright file="TenantRuntimeOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;

    /// <summary>
    /// Host-level limits for tenant configuration and cache maintenance.
    /// </summary>
    public sealed class TenantRuntimeOptions
    {
        /// <summary>Gets or sets the maximum duration of a configuration load.</summary>
        public TimeSpan ConfigurationLoadTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>Gets or sets the first retry delay after a failed load.</summary>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Gets or sets the maximum retry delay.</summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>Gets or sets the interval between successful asset refreshes.</summary>
        public TimeSpan StaticAssetRefreshInterval { get; set; } = TimeSpan.FromDays(1);

        /// <summary>Gets or sets the shared invalidation polling interval.</summary>
        public TimeSpan InvalidationPollInterval { get; set; } = TimeSpan.FromDays(1);

        /// <summary>Checks that maintenance intervals are positive and bounded.</summary>
        /// <returns>Whether all configured intervals are valid.</returns>
        public bool IsValid()
        {
            return this.ConfigurationLoadTimeout > TimeSpan.Zero && this.ConfigurationLoadTimeout <= TimeSpan.FromMinutes(5)
                && this.InitialRetryDelay > TimeSpan.Zero && this.MaxRetryDelay >= this.InitialRetryDelay
                && this.MaxRetryDelay <= TimeSpan.FromMinutes(5)
                && this.StaticAssetRefreshInterval > TimeSpan.Zero && this.StaticAssetRefreshInterval <= TimeSpan.FromDays(30)
                && this.InvalidationPollInterval > TimeSpan.Zero && this.InvalidationPollInterval <= TimeSpan.FromDays(1);
        }

        /// <summary>Calculates bounded exponential retry backoff.</summary>
        /// <param name="failureCount">Consecutive failures.</param>
        /// <returns>The delay before retrying.</returns>
        public TimeSpan GetRetryDelay(int failureCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(failureCount);
            return TimeSpan.FromTicks((long)Math.Min(
                this.MaxRetryDelay.Ticks,
                this.InitialRetryDelay.Ticks * Math.Pow(2, Math.Clamp(failureCount - 1, 0, 30))));
        }
    }
}
