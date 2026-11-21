namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using Microsoft.AspNetCore.Authentication.OAuth;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Separates named authentication options by the current tenant.
    /// </summary>
    /// <typeparam name="TOptions">Authentication options type.</typeparam>
    public class TenantAuthenticationOptionsCache<TOptions> : IOptionsMonitorCache<TOptions>, ITenantAuthenticationOptionsCache
        where TOptions : class, new()
    {
        private readonly ConcurrentDictionary<int, CacheEntry> caches = new ConcurrentDictionary<int, CacheEntry>();
        private readonly IHttpContextAccessor context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantAuthenticationOptionsCache{TOptions}"/> class.
        /// </summary>
        /// <param name="context">Current request accessor.</param>
        public TenantAuthenticationOptionsCache(IHttpContextAccessor context)
        {
            this.context = context;
        }

        /// <inheritdoc/>
        public TOptions GetOrAdd(string? name, Func<TOptions> createOptions)
        {
            int tenantId = this.GetTenantId();
            return this.GetCurrentCache().GetOrAdd(name, () =>
            {
                TOptions options = createOptions();
                if (options is OAuthOptions oauthOptions && oauthOptions.StateDataFormat != null)
                {
                    oauthOptions.StateDataFormat = new TenantAuthenticationPropertiesFormat(oauthOptions.StateDataFormat, tenantId);
                }

                return options;
            });
        }

        /// <inheritdoc/>
        public bool TryAdd(string? name, TOptions options)
        {
            return this.GetCurrentCache().TryAdd(name, options);
        }

        /// <inheritdoc/>
        public bool TryRemove(string? name)
        {
            if (this.context.HttpContext?.Features.Get<Model.Tenant>() != null)
            {
                return this.GetCurrentCache().TryRemove(name);
            }

            bool removed = false;
            foreach (CacheEntry cache in this.caches.Values)
            {
                removed = cache.Options.TryRemove(name) || removed;
            }

            return removed;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.caches.Clear();
        }

        private OptionsCache<TOptions> GetCurrentCache()
        {
            IDictionary<string, string>? source = this.context.HttpContext?.GetTenantSpecificConfiguration();
            return this.caches.AddOrUpdate(
                this.GetTenantId(),
                _ => new CacheEntry(source),
                (_, existing) => ReferenceEquals(existing.Source, source) ? existing : new CacheEntry(source)).Options;
        }

        private int GetTenantId()
        {
            return this.context.HttpContext?.Features.Get<Model.Tenant>()?.Id ?? 0;
        }

        private sealed class CacheEntry
        {
            public CacheEntry(IDictionary<string, string>? source)
            {
                this.Source = source;
            }

            public IDictionary<string, string>? Source { get; }

            public OptionsCache<TOptions> Options { get; } = new OptionsCache<TOptions>();
        }
    }
}