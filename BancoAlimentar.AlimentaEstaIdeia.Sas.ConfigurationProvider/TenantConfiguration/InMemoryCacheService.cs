// -----------------------------------------------------------------------
// <copyright file="InMemmoryCacheService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to hold the Tenant configuration in memory using WeakReference.
    /// </summary>
    public class InMemoryCacheService
    {
        private readonly Dictionary<int, Dictionary<string, string>> cache;
        private readonly ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

        public InMemoryCacheService()
        {
            cache = new Dictionary<int, Dictionary<string, string>>();
        }

        /// <summary>
        /// Gets the Tenant configuration from the memory.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns>A reference to the <see cref="Dictionary{TKey, TValue}"/>.</returns>
        public Dictionary<string, string> GetTenantConfiguration(int tenantId)
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                if (cache.ContainsKey(tenantId))
                {
                    return cache[tenantId];
                }
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }

            return null;
        }

        /// <summary>
        /// Sets the Tenant configuration in the memory.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="configuration">Dictionary with the values.</param>
        public void SetTenantConfiguration(int tenantId, Dictionary<string, string> configuration)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                this.cache.Add(tenantId, configuration);
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}
