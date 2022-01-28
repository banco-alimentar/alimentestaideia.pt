// -----------------------------------------------------------------------
// <copyright file="DoarConfigurationProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Microsoft.Extensions.Primitives;

    /// <summary>
    ///  This is the Doar SaS configuration provider that map the current tenant to their configuration.
    /// </summary>
    public class DoarConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
    {
        private static DoarConfigurationProvider instance = new DoarConfigurationProvider();

        /// <summary>
        /// Initializes a new instance of the <see cref="DoarConfigurationProvider"/> class.
        /// </summary>
        private DoarConfigurationProvider()
        {
        }

        /// <summary>
        /// Gets the default instance of the object.
        /// </summary>
        public static DoarConfigurationProvider Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Gets or sets the HttpContext.
        /// </summary>
        public HttpContext? HttpContext { get; set; }

        /// <inheritdoc/>
        public override void Load()
        {
            base.Load();
        }

        /// <inheritdoc/>
        public override bool TryGet(string key, out string value)
        {
            if (!string.IsNullOrEmpty(key) && key == "Tenant-Name")
            {
                if (this.HttpContext != null)
                {
                    Model.Tenant? tenant = this.HttpContext.Items["__Tenant"] as Model.Tenant;
                    if (tenant != null)
                    {
                        value = tenant.DomainIdentifier;
                        return true;
                    }
                    else
                    {
                        value = string.Empty;
                        return false;
                    }
                }
            }

            return base.TryGet(key, out value);
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            return base.GetChildKeys(earlierKeys, parentPath);
        }
    }
}
