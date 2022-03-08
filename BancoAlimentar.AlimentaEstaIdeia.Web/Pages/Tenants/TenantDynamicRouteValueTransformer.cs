// -----------------------------------------------------------------------
// <copyright file="TenantDynamicRouteValueTransformer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Tenant dynamic route value transformer.
    /// </summary>
    public class TenantDynamicRouteValueTransformer : DynamicRouteValueTransformer
    {
        /// <summary>
        /// Tranform route.
        /// </summary>
        /// <param name="httpContext">Current http context.</param>
        /// <param name="values">Current parameter values.</param>
        /// <returns>Route values.</returns>
        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            var routes = new RouteValueDictionary();

            // routes.Add("page", "/Index");
            return new ValueTask<RouteValueDictionary>(routes);
        }
    }
}
