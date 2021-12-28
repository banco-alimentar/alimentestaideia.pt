// -----------------------------------------------------------------------
// <copyright file="DoarTenantMiddlewareExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;

    /// <summary>
    /// Doar+ middleware extension.
    /// </summary>
    public static class DoarTenantMiddlewareExtensions
    {
        /// <summary>
        /// Add the Doar+ multitenancy to the system.
        /// </summary>
        /// <param name="builder">A reference to the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A reference to the same<see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseDoarMultitenancy(
            this IApplicationBuilder builder)
            {
                return builder.UseMiddleware<DoarTenantMiddleware>();
            }
    }
}
