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
        private long generation;

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
                if (this.cache.ContainsKey(tenantId))
                {
                    this.cache[tenantId] = configuration;
                }
                else
                {
                    this.cache.Add(tenantId, configuration);
                }
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the current cache generation. Capture it before querying the database and pass it to
        /// <see cref="SetTenantConfiguration(int, Dictionary{string, string}, long)"/>.
        /// </summary>
        public long Generation => Interlocked.Read(ref this.generation);

        /// <summary>
        /// Sets the Tenant configuration only when the cache has not been cleared since
        /// <paramref name="expectedGeneration"/> was captured, so in-flight queries cannot restore pre-reload values.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="configuration">Dictionary with the values.</param>
        /// <param name="expectedGeneration">Generation captured before the values were read.</param>
        /// <returns>True when the values were cached; false when the cache was invalidated meanwhile.</returns>
        public bool SetTenantConfiguration(int tenantId, Dictionary<string, string> configuration, long expectedGeneration)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                if (this.generation != expectedGeneration)
                {
                    return false;
                }

                this.cache[tenantId] = configuration;
                return true;
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clears all tenant configurations from memory and invalidates in-flight writes.
        /// </summary>
        public void Clear()
        {
            this.readerWriterLockSlim.EnterWriteLock();
            try
            {
                this.cache.Clear();
                Interlocked.Increment(ref this.generation);
            }
            finally
            {
                this.readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}
