// -----------------------------------------------------------------------
// <copyright file="LocalDevelopmentOverride.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// This class handle the Tenant override for the local development in localhost.
    /// </summary>
    public class LocalDevelopmentOverride
    {
        private const string TenantOverrideKey = "Tenant-Override";
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDevelopmentOverride"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public LocalDevelopmentOverride(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        internal TenantData GetOverrideTenantName()
        {
            string tenantOverride = this.configuration[TenantOverrideKey];
            if (!string.IsNullOrEmpty(tenantOverride))
            {
                return new TenantData(tenantOverride, true);
            }

            return new TenantData("localhost", true);
        }
    }
}
