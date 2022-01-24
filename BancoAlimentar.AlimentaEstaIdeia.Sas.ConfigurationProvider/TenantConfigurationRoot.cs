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
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// TenantConfigurationRoot that implements <see cref="IConfiguration"/>.
    /// </summary>
    public class TenantConfigurationRoot : IConfiguration
    {
        private readonly IConfiguration root;
        private readonly IHttpContextAccessor context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantConfigurationRoot"/> class.
        /// </summary>
        /// <param name="root">Root configuration.</param>
        /// <param name="context">Http context.</param>
        public TenantConfigurationRoot(IConfiguration root, IHttpContextAccessor context)
        {
            this.root = root;
            this.context = context;
        }

        /// <inheritdoc/>
        public string this[string key]
        {
            get
            {
                HttpContext current = this.context.HttpContext;
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
