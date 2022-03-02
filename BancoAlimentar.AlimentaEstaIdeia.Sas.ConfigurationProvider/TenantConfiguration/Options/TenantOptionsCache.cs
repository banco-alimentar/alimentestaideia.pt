// -----------------------------------------------------------------------
// <copyright file="TenantOptionsCache.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Tenant aware options cache.
    /// </summary>
    /// <typeparam name="TOptions">Options.</typeparam>
    public class TenantOptionsCache<TOptions> : IOptionsMonitorCache<TOptions>
        where TOptions : class
    {
        private readonly TenantOptionsCacheDictionary<TOptions> tenantSpecificOptionsCache =
            new TenantOptionsCacheDictionary<TOptions>();

        private readonly IHttpContextAccessor contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantOptionsCache{TOptions}"/> class.
        /// </summary>
        /// <param name="contextAccessor">Http context.</param>
        public TenantOptionsCache(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        public void Clear()
        {
            if (this.contextAccessor.HttpContext != null)
            {
                Tenant tenant = this.contextAccessor.HttpContext.GetTenant();
                this.tenantSpecificOptionsCache.Get(tenant.Id).Clear();
            }
        }

        /// <summary>
        /// Gets or add the value.
        /// </summary>
        /// <param name="name">Name of the options.</param>
        /// <param name="createOptions">Factory to create the options.</param>
        /// <returns>A reference to the Option.</returns>
        public TOptions GetOrAdd(string name, Func<TOptions> createOptions)
        {
            if (this.contextAccessor.HttpContext != null)
            {
                Tenant tenant = this.contextAccessor.HttpContext.GetTenant();
                return this.tenantSpecificOptionsCache.Get(tenant.Id)
                    .GetOrAdd(name, createOptions);
            }
            else
            {
                return createOptions();
            }
        }

        /// <summary>
        /// Try to add the option.
        /// </summary>
        /// <param name="name">NAme of the option.</param>
        /// <param name="options">Option value.</param>
        /// <returns>True if added, false otherwise.</returns>
        public bool TryAdd(string name, TOptions options)
        {
            bool result = false;
            if (this.contextAccessor.HttpContext != null)
            {
                Tenant tenant = this.contextAccessor.HttpContext.GetTenant();
                result = this.tenantSpecificOptionsCache.Get(tenant.Id)
                    .TryAdd(name, options);
            }

            return result;
        }

        /// <summary>
        /// Try removing the tenant.
        /// </summary>
        /// <param name="name">Name of the option.</param>
        /// <returns>True if the option is being removed.</returns>
        public bool TryRemove(string name)
        {
            bool result = false;
            if (this.contextAccessor.HttpContext != null)
            {
                Tenant tenant = this.contextAccessor.HttpContext.GetTenant();
                result = this.tenantSpecificOptionsCache.Get(tenant.Id)
                    .TryRemove(name);
            }

            return result;
        }
    }
}
