// -----------------------------------------------------------------------
// <copyright file="DoarTenantEndpointSelector.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Matching;

    /// <summary>
    /// Implements the Doar Multitenancy endpoint selector.
    /// </summary>
    public class DoarTenantEndpointSelector : EndpointSelector
    {
        private EndpointSelector defaultEndpointSelector;
        private EndpointDataSource endpointDataSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoarTenantEndpointSelector"/> class.
        /// </summary>
        /// <param name="defaultEndpointSelector">Default Endpoint Selector.</param>
        /// <param name="endpointDataSource">Current Endpoint Data Source.</param>
        public DoarTenantEndpointSelector(EndpointSelector defaultEndpointSelector, EndpointDataSource endpointDataSource)
        {
            this.defaultEndpointSelector = defaultEndpointSelector;
            this.endpointDataSource = endpointDataSource;
        }

        /// <inheritdoc/>
        public override async Task SelectAsync(HttpContext httpContext, CandidateSet candidates)
        {
            await this.defaultEndpointSelector.SelectAsync(httpContext, candidates);
            Endpoint? currentEndpoint = httpContext.GetEndpoint();
            if (currentEndpoint != null)
            {
                Model.Tenant currentTenant = httpContext.GetTenant();
                string tenanName = currentTenant.Name.ToLowerInvariant();
                string? endPointName = currentEndpoint.DisplayName?.ToLowerInvariant();
                Endpoint? targetEndpoint = this.endpointDataSource.Endpoints
                    .Where(p => p.DisplayName?.ToLowerInvariant() == $"/tenants/{tenanName}/pages{endPointName}")
                    .FirstOrDefault();
                if (targetEndpoint != null)
                {
                    httpContext.SetEndpoint(targetEndpoint);
                }
            }
        }
    }
}
