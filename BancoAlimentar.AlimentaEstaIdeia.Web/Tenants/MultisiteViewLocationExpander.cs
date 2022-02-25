// -----------------------------------------------------------------------
// <copyright file="MultisiteViewLocationExpander.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Tenants
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using global::BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using global::BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.Graph;

    /// <summary>
    /// Multisite view location expander.
    /// </summary>
    public class MultisiteViewLocationExpander : IViewLocationExpander
    {
        private const string TenantKey = "tenant";

        /// <summary>
        /// Populate view.
        /// </summary>
        /// <param name="context">Context.</param>
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            Tenant tenant = context.ActionContext.HttpContext.GetTenant();
            if (tenant != null)
            {
                context.Values[TenantKey] = tenant.Name;
            }
        }

        /// <summary>
        /// Expands views.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="viewLocations">View locations.</param>
        /// <returns>The collection.</returns>
        public IEnumerable<string> ExpandViewLocations(
            ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            string tenant = null;
            if (context.Values.TryGetValue(TenantKey, out tenant))
            {
                IEnumerable<string> tenantLocations = new[]
                {
                    $"/Tenants/{tenant}/Pages/{{0}}.cshtml",
                    $"/Tenants/{tenant}/Pages/Shared/{{0}}.cshtml",
                };
                viewLocations = tenantLocations.Concat(viewLocations);
            }

            Debug.WriteLine(string.Join(Environment.NewLine, viewLocations));

            return viewLocations;
        }

        private IEnumerable<string> ExpandTenantLocations(string tenant, IEnumerable<string> defaultLocations)
        {
            foreach (var location in defaultLocations)
            {
                yield return location.Replace("{0}", $"{{0}}_{tenant}");
                yield return location;
            }
        }
    }
}
