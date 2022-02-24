// -----------------------------------------------------------------------
// <copyright file="ITenantProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Define the tenant provider the get information and excecute operations for the tenant.
    /// </summary>
    public interface ITenantProvider
    {
        /// <summary>
        /// Gets the current tenant data.
        /// </summary>
        /// <param name="context">Current http context.</param>
        /// <returns>A reference to the <see cref="TenantData"/>.</returns>
        TenantData GetTenantData(HttpContext context);
    }
}
