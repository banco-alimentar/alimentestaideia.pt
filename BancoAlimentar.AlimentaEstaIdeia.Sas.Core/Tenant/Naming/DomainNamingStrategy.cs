// -----------------------------------------------------------------------
// <copyright file="DomainNamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Domain name strategy.
/// </summary>
public class DomainNamingStrategy : INamingStrategy
{
    /// <inheritdoc/>
    public TenantData GetTenantName(HttpContext httpContext)
    {
        return new TenantData(httpContext.Request.Host.Host, false);
    }
}
