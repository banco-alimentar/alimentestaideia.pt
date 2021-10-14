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
        private readonly UserManager<WebUser> userManager;
        private readonly RoleManager<ApplicationUserRole> roleManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetingContextAccessor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The current http context accesor.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="roleManager">Role manager.</param>
        public TargetingContextAccessor(
            IHttpContextAccessor httpContextAccessor,
            UserManager<WebUser> userManager,
            RoleManager<ApplicationUserRole> roleManager)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            this.userManager = userManager;
            this.roleManager = roleManager;
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

            WebUser user = userManager.GetUserAsync((ClaimsPrincipal)httpContext.User.Identity).Result;

            List<string> groups = new List<string>();
            if (httpContext.User.Identity.Name != null)
            {
                groups.Add(httpContext.User.Identity.Name.Split("@", StringSplitOptions.None)[1]);
            }

            TargetingContext targetingContext = new TargetingContext
            {
                UserId = user.Email,
                Groups = groups,
            };
            httpContext.Items[TargetingContextLookup] = targetingContext;
            return new ValueTask<TargetingContext>(targetingContext);
        }
    }
}
