// -----------------------------------------------------------------------
// <copyright file="TenantCookieEventsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    /// <summary>
    /// Tests tenant binding for Identity application cookies.
    /// </summary>
    public class TenantCookieEventsTests
    {
        private const string SchemeName = "Identity.Application";

        /// <summary>
        /// Adds the current tenant ID when an application cookie is issued.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task SigningIn_AddsCurrentTenantClaim()
        {
            ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "user-id") },
                SchemeName));
            DefaultHttpContext httpContext = CreateTenantContext(7);
            CookieSigningInContext context = new CookieSigningInContext(
                httpContext,
                CreateScheme(),
                new CookieAuthenticationOptions(),
                principal,
                new AuthenticationProperties(),
                new CookieOptions());

            await new TenantCookieEvents().SigningIn(context);

            Assert.Equal("7", principal.FindFirst(TenantCookieEvents.TenantClaimType)?.Value);
        }

        /// <summary>
        /// Rejects a cookie issued for a different tenant.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ValidatePrincipal_RejectsCookieForDifferentTenant()
        {
            ClaimsPrincipal principal = CreatePrincipal("6");
            DefaultHttpContext httpContext = CreateTenantContext(7);
            CookieValidatePrincipalContext context = new CookieValidatePrincipalContext(
                httpContext,
                CreateScheme(),
                new CookieAuthenticationOptions(),
                new AuthenticationTicket(principal, SchemeName));

            await new TenantCookieEvents().ValidatePrincipal(context);

            Assert.Null(context.Principal);
        }

        /// <summary>
        /// Keeps a cookie issued for the current tenant valid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task ValidatePrincipal_AcceptsCookieForCurrentTenant()
        {
            ClaimsPrincipal principal = CreatePrincipal("7");
            DefaultHttpContext httpContext = CreateTenantContext(7);
            CookieValidatePrincipalContext context = new CookieValidatePrincipalContext(
                httpContext,
                CreateScheme(),
                new CookieAuthenticationOptions(),
                new AuthenticationTicket(principal, SchemeName));

            await new TenantCookieEvents().ValidatePrincipal(context);

            Assert.Same(principal, context.Principal);
        }

        private static AuthenticationScheme CreateScheme()
        {
            return new AuthenticationScheme(
                SchemeName,
                SchemeName,
                typeof(CookieAuthenticationHandler));
        }

        private static ClaimsPrincipal CreatePrincipal(string tenantId)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-id"),
                    new Claim(TenantCookieEvents.TenantClaimType, tenantId),
                },
                SchemeName));
        }

        private static DefaultHttpContext CreateTenantContext(int tenantId)
        {
            DefaultHttpContext context = new DefaultHttpContext();
            context.SetTenant(new Tenant { Id = tenantId });
            return context;
        }
    }
}
