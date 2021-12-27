// -----------------------------------------------------------------------
// <copyright file="PathNamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming
{
    using System;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Path naming strategy.
    /// </summary>
    public class PathNamingStrategy : INamingStrategy
    {
        /// <inheritdoc/>
        public TenantData GetTenantName(IHttpContextAccessor httpContext)
        {
            Uri value = new Uri(httpContext.HttpContext.Request.Path.ToUriComponent());
            return new TenantData(value.Segments.Skip(1).Take(1).First());
        }
    }
}
