// -----------------------------------------------------------------------
// <copyright file="TenantProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant;

using System.Collections.Generic;
using System.Linq;
using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;
using Microsoft.AspNetCore.Http;

/// <inheritdoc/>
public class TenantProvider : ITenantProvider
{
    private readonly IEnumerable<INamingStrategy> strategies;
    private readonly LocalDevelopmentOverride localDevelopmentOverride;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvider"/> class.
    /// </summary>
    /// <param name="strategies">Registered strategies in the system.</param>
    /// <param name="localDevelopmentOverride">Local development override tenant name.</param>
    public TenantProvider(
        IEnumerable<INamingStrategy> strategies,
        LocalDevelopmentOverride localDevelopmentOverride)
    {
        this.strategies = strategies;
        this.localDevelopmentOverride = localDevelopmentOverride;
    }

    /// <inheritdoc/>
    public TenantData GetTenantData(HttpContext context)
    {
        if (context.Request.Host.Host == "localhost")
        {
            return this.localDevelopmentOverride.GetOverrideTenantName();
        }
        else
        {
            INamingStrategy tenantNaming = this.strategies
            .Where(p => !string.IsNullOrEmpty(p.GetTenantName(context).Name))
            .First();

            return tenantNaming.GetTenantName(context);
        }
    }
}
