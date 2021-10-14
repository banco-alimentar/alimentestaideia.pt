// -----------------------------------------------------------------------
// <copyright file="TargetingContextAccessor.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.FeatureManagement;
    using Microsoft.FeatureManagement.FeatureFilters;

    /// <summary>
    /// Extract the username and group for the Feature Manager Targeting filter.
    /// </summary>
    public class TargetingContextAccessor : ITargetingContextAccessor
    {
        private const string TargetingContextLookup = "TestTargetingContextAccessor.TargetingContext";
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetingContextAccessor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The current http context accesor.</param>
        public TargetingContextAccessor(
            IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the current Targeting context.
        /// </summary>
        /// <returns>A reference to the <see cref="TargetingContext"/> instance.</returns>
        public ValueTask<TargetingContext> GetContextAsync()
        {
            HttpContext httpContext = httpContextAccessor.HttpContext;
            if (httpContext.Items.TryGetValue(TargetingContextLookup, out object value))
            {
                return new ValueTask<TargetingContext>((TargetingContext)value);
            }

            TargetingContext targetingContext = new TargetingContext
            {
                UserId = GetUserEmail(httpContext.User.Identity as ClaimsIdentity),
                Groups = GetRoles(httpContext.User.Identity as ClaimsIdentity),
            };
            httpContext.Items[TargetingContextLookup] = targetingContext;
            return new ValueTask<TargetingContext>(targetingContext);
        }

        private IEnumerable<string> GetRoles(ClaimsIdentity identity)
        {
            List<string> result = new List<string>();

            if (identity != null)
            {
                var roleClaims = identity.Claims.Where(p => p.Type == ClaimTypes.Role).ToList();
                foreach (var item in roleClaims)
                {
                    result.Add(item.Value);
                }
            }

            return result;
        }

        private string GetUserEmail(ClaimsIdentity identity)
        {
            string result = null;

            if (identity != null)
            {
                var emailClaim = identity.FindFirst(ClaimTypes.Email);
                if (emailClaim != null)
                {
                    result = emailClaim.Value;
                }
            }

            return result;
        }
    }
}
