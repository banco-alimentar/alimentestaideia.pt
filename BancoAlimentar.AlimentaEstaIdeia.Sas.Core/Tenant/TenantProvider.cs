// -----------------------------------------------------------------------
// <copyright file="TenantProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
    using Microsoft.AspNetCore.Http;

    /// <inheritdoc/>
    public class TenantProvider : ITenantProvider
    {
        private readonly IEnumerable<INamingStrategy> strategies;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantProvider"/> class.
        /// </summary>
        /// <param name="strategies">Registered strategies in the system.</param>
        public TenantProvider(IEnumerable<INamingStrategy> strategies)
        {
            this.strategies = strategies;
        }

        /// <inheritdoc/>
        public TenantData GetTenantData(HttpContext context)
        {
            INamingStrategy tenantNaming = this.strategies
                .Where(p => !string.IsNullOrEmpty(p.GetTenantName(context).Name))
                .First();

            return tenantNaming.GetTenantName(context);
        }
    }
}
