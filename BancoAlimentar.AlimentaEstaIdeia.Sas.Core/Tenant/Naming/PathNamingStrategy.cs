// -----------------------------------------------------------------------
// <copyright file="PathNamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Extensions;

    /// <summary>
    /// Path naming strategy.
    /// </summary>
    public class PathNamingStrategy : INamingStrategy
    {
        /// <inheritdoc/>
        public TenantData GetTenantName(HttpContext httpContext)
        {
            Uri value = new Uri(httpContext.Request.GetDisplayUrl());
            if (value.Segments.Length > 2)
            {
                string firstSegment = value.Segments.ElementAt(1).Replace("/", string.Empty);
                return new TenantData(firstSegment, false);
            }
            else
            {
                return new TenantData(string.Empty, false);
            }
        }
    }
}
