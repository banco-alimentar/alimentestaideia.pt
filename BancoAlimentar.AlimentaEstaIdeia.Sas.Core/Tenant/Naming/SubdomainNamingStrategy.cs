// -----------------------------------------------------------------------
// <copyright file="SubdomainNamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Naming strategy that extract the name of the tenant from the subdomain.
/// </summary>
public class SubdomainNamingStrategy : INamingStrategy
{
    private readonly string baseDomain;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubdomainNamingStrategy"/> class.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    public SubdomainNamingStrategy(IConfiguration configuration)
    {
        this.baseDomain = configuration["SAS-BaseDomain"];
    }

    /// <inheritdoc/>
    public TenantData GetTenantName(HttpContext httpContext)
    {
        string hostName = httpContext.Request.Host.Host;
        hostName = hostName.Replace(this.baseDomain, string.Empty);
        if (!string.IsNullOrEmpty(hostName) && hostName.Last() == '.')
        {
            hostName = hostName.Substring(0, hostName.Length - 1);
        }

        return new TenantData(hostName, false);
    }
}
