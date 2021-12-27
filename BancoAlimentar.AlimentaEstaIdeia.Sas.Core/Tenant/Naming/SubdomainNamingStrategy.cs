// -----------------------------------------------------------------------
// <copyright file="SubdomainNamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Naming strategy that extract the name of the tenant from the subdomain.
    /// </summary>
    public class SubdomainNamingStrategy : INamingStrategy
    {
        private readonly string baseDomain;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubdomainNamingStrategy"/> class.
        /// </summary>
        /// <param name="baseDomain">Base domain.</param>
        public SubdomainNamingStrategy(string baseDomain)
        {
            this.baseDomain = baseDomain;
        }

        /// <inheritdoc/>
        public TenantData GetTenantName(IHttpContextAccessor httpContext)
        {
            string hostName = httpContext.HttpContext.Request.Host.Host;
            hostName = hostName.Replace(this.baseDomain, string.Empty);
            if (!string.IsNullOrEmpty(hostName) && hostName.Last() == '.')
            {
                hostName = hostName.Substring(0, hostName.Length - 1);
            }

            return new TenantData(hostName);
        }
    }
}
