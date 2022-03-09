// -----------------------------------------------------------------------
// <copyright file="TenantOptionsCacheDictionary.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options
{
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Dictionary of tenant specific options caches.
    /// </summary>
    /// <typeparam name="TOptions">Options.</typeparam>
    public class TenantOptionsCacheDictionary<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Caches stored in memory.
        /// </summary>
        private readonly ConcurrentDictionary<int, IOptionsMonitorCache<TOptions>> tenantSpecificOptionCaches =
            new ConcurrentDictionary<int, IOptionsMonitorCache<TOptions>>();

        /// <summary>
        /// Get options for specific tenant (create if not exists).
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <returns>Cache value.</returns>
        public IOptionsMonitorCache<TOptions> Get(int tenantId)
        {
            return this.tenantSpecificOptionCaches.GetOrAdd(tenantId, new OptionsCache<TOptions>());
        }
    }
}