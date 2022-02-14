// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationRoot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// TenantConfigurationRoot that implements <see cref="IConfiguration"/>.
    /// </summary>
    public class TenantConfigurationRoot : IConfiguration
    {
        private readonly IConfiguration root;
        private readonly IHttpContextAccessor context;
        private readonly IServiceCollection serviceCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantConfigurationRoot"/> class.
        /// </summary>
        /// <param name="root">Root configuration.</param>
        /// <param name="context">Http context.</param>
        /// <param name="serviceCollection">Service Collection.</param>
        public TenantConfigurationRoot(
            IConfiguration root,
            IHttpContextAccessor context,
            IServiceCollection serviceCollection)
        {
            this.root = root;
            this.context = context;
            this.serviceCollection = serviceCollection;
        }

        /// <inheritdoc/>
        public string this[string key]
        {
            get
            {
                HttpContext? current = this.context.HttpContext;
                if (current != null)
                {
                    IDictionary<string, string>? tenantConfiguration = current.GetTenantSpecificConfiguration();
                    if (tenantConfiguration != null)
                    {
                        if (tenantConfiguration.ContainsKey(key))
                        {
                            return tenantConfiguration[key];
                        }
                    }
                }

                return this.root[key];
            }

            set
            {
                this.root[key] = value;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return this.root.GetChildren();
        }

        /// <inheritdoc/>
        public IChangeToken GetReloadToken()
        {
            return this.root.GetReloadToken();
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key)
        {
            return this.root.GetSection(key);
        }
    }
}
