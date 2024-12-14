// -----------------------------------------------------------------------
// <copyright file="TenantHttpContextAccessor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Http
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Represents the tenant HTTP context accessor.
    /// </summary>
    public class TenantHttpContextAccessor : IHttpContextAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantHttpContextAccessor"/> class.
        /// </summary>
        public TenantHttpContextAccessor(TenantHttpNonHttpContext httpContext)
        {
            this.HttpContext = httpContext;
        }

        /// <summary>
        /// Gets or sets the current HTTP context.
        /// </summary>
        public HttpContext? HttpContext { get; set; }
    }
}
