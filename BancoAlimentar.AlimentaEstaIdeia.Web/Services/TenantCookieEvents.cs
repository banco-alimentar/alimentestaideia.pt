// -----------------------------------------------------------------------
// <copyright file="TenantCookieEvents.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System.Globalization;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Authentication.Cookies;

    /// <summary>
    /// Binds the Identity application cookie to the tenant that issued it.
    /// </summary>
    public sealed class TenantCookieEvents : CookieAuthenticationEvents
    {
        /// <summary>
        /// Claim containing the tenant ID associated with an application cookie.
        /// </summary>
        public const string TenantClaimType = "urn:alimentestaideia:tenant-id";

        /// <summary>
        /// Adds the current tenant to a newly issued application cookie.
        /// </summary>
        /// <param name="context">Cookie signing-in context.</param>
        /// <returns>A completed task.</returns>
        public override Task SigningIn(CookieSigningInContext context)
        {
            Tenant tenant = context.HttpContext.GetTenant();
            ClaimsIdentity? identity = context.Principal?.Identity as ClaimsIdentity;
            if (tenant.Id > 0 && identity != null)
            {
                Claim? existingTenantClaim = identity.FindFirst(TenantClaimType);
                if (existingTenantClaim != null)
                {
                    identity.RemoveClaim(existingTenantClaim);
                }

                identity.AddClaim(new Claim(
                    TenantClaimType,
                    tenant.Id.ToString(CultureInfo.InvariantCulture)));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Rejects an application cookie issued for another tenant.
        /// </summary>
        /// <param name="context">Cookie validation context.</param>
        /// <returns>A completed task.</returns>
        public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            Tenant tenant = context.HttpContext.GetTenant();
            string? tenantClaim = context.Principal?.FindFirst(TenantClaimType)?.Value;
            bool validTenantCookie = tenant.Id > 0
                && int.TryParse(
                    tenantClaim,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int cookieTenantId)
                && cookieTenantId == tenant.Id;

            if (!validTenantCookie)
            {
                context.RejectPrincipal();
            }

            return Task.CompletedTask;
        }
    }
}
