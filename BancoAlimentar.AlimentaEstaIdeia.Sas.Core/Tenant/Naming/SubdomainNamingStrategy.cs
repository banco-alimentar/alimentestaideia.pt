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
        private readonly IHttpContextAccessor httpContext;
        private readonly string baseDomain;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubdomainNamingStrategy"/> class.
        /// </summary>
        /// <param name="httpContext">Http context accesor.</param>
        /// <param name="baseDomain">Base domain.</param>
        public SubdomainNamingStrategy(IHttpContextAccessor httpContext, string baseDomain)
        {
            this.httpContext = httpContext;
            this.baseDomain = baseDomain;
        }

        /// <inheritdoc/>
        public TenantData GetTenantName()
        {
            string hostName = this.httpContext.HttpContext.Request.Host.Host;
            hostName = hostName.Replace(string.Concat(".", this.baseDomain), string.Empty);
            return new TenantData(hostName);
        }
    }
}
