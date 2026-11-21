// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfigurationCache.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

    /// <summary>
    /// Coordinates configuration loading across managers belonging to one application host.
    /// </summary>
    public sealed class KeyVaultConfigurationCache
    {
        private State state = new State();

        internal State Current => Volatile.Read(ref this.state);

        /// <summary>Gets the current local cache generation.</summary>
        public Guid Generation => this.Current.Generation;

        /// <summary>Gets a token canceled when the current generation is invalidated.</summary>
        public CancellationToken ReloadToken => this.Current.Reload.Token;

        /// <summary>Reads tenant readiness without exposing credentials or error messages.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <returns>The current readiness snapshot.</returns>
        public TenantConfigurationStatus GetStatus(int tenantId)
        {
            State current = this.Current;
            current.Failures.TryGetValue(tenantId, out Failure? failure);
            failure ??= current.BootstrapFailure;
            return new TenantConfigurationStatus(
                tenantId,
                current.Tenants.TryGetValue(tenantId, out Tenant? tenant) ? tenant.Name : null,
                current.Generation,
                current.Initialized,
                current.Clients.ContainsKey(tenantId),
                current.Values.ContainsKey(tenantId),
                current.LoadedAt.TryGetValue(tenantId, out DateTimeOffset loadedAt) ? loadedAt : null,
                failure?.Diagnostics.Stage,
                failure?.RetryAt);
        }

        /// <summary>Gets diagnostic snapshots for tenants known to this instance.</summary>
        /// <returns>Secret-free tenant configuration status.</returns>
        public IReadOnlyList<TenantConfigurationStatus> GetStatuses()
        {
            State current = this.Current;
            return current.Tenants.Keys.Concat(current.Values.Keys).Concat(current.Failures.Keys)
                .Distinct().OrderBy(id => id).Select(this.GetStatus).ToList();
        }

        /// <summary>
        /// Invalidates configuration without letting in-flight loads repopulate the new cache.
        /// </summary>
        public void Clear()
        {
            State previous = Interlocked.Exchange(ref this.state, new State());
            previous.Reload.Cancel();
        }

        internal sealed class State
        {
            public Guid Generation { get; } = Guid.NewGuid();

            public CancellationTokenSource Reload { get; } = new CancellationTokenSource();

            public SemaphoreSlim BootstrapLock { get; } = new SemaphoreSlim(1, 1);

            public bool Initialized { get; set; }

            public Dictionary<string, string> CoreSecrets { get; set; } = new Dictionary<string, string>();

            public ConcurrentDictionary<int, Tenant> Tenants { get; } = new ConcurrentDictionary<int, Tenant>();

            public ConcurrentDictionary<int, SecretClient> Clients { get; } = new ConcurrentDictionary<int, SecretClient>();

            public ConcurrentDictionary<int, Dictionary<string, string>> Values { get; } = new ConcurrentDictionary<int, Dictionary<string, string>>();

            public ConcurrentDictionary<int, SemaphoreSlim> TenantLocks { get; } = new ConcurrentDictionary<int, SemaphoreSlim>();

            public ConcurrentDictionary<int, DateTimeOffset> LoadedAt { get; } = new ConcurrentDictionary<int, DateTimeOffset>();

            public ConcurrentDictionary<int, Failure> Failures { get; } = new ConcurrentDictionary<int, Failure>();

            public Failure? BootstrapFailure { get; set; }
        }

        internal sealed record Failure(KeyVaultConfigurationLoadDiagnostics Diagnostics, int Attempts, DateTimeOffset RetryAt);
    }
}
