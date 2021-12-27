// -----------------------------------------------------------------------
// <copyright file="INamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Naming strategy pattert to get the name of the tenant.
    /// </summary>
    public interface INamingStrategy
    {
        /// <summary>
        /// Gets the name of the tentant.
        /// </summary>
        /// <param name="httpContext">Http context accessor.</param>
        /// <returns>The name of the tenant.</returns>
        TenantData GetTenantName(IHttpContextAccessor httpContext);
    }
}
