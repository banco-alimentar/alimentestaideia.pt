namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using System;
    using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<int, OptionsCache<TOptions>> caches = new ConcurrentDictionary<int, OptionsCache<TOptions>>();
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
            return this.caches.GetOrAdd(tenantId, _ => new OptionsCache<TOptions>()).GetOrAdd(name, () =>
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
            foreach (OptionsCache<TOptions> cache in this.caches.Values)
            {
                removed = cache.TryRemove(name) || removed;
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
            return this.caches.GetOrAdd(this.GetTenantId(), _ => new OptionsCache<TOptions>());
        }

        private int GetTenantId()
        {
            return this.context.HttpContext?.Features.Get<Model.Tenant>()?.Id ?? 0;
        }
    }
}