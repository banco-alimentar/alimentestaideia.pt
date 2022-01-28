// -----------------------------------------------------------------------
// <copyright file="HttpContextExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// <see cref="HttpContext"/> extensions.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the key for the tenant value in the <see cref="HttpContext"/>.
        /// </summary>
        public const string TenantKey = "__Tenant";

        /// <summary>
        /// Gets the key for the tenant ID in the <see cref="HttpContext"/>.
        /// </summary>
        public const string TenantIdKey = "__TenantId";

        /// <summary>
        /// Gets the tenant data.
        /// </summary>
        /// <param name="value">A reference to the <see cref="HttpContext"/>.</param>
        /// <returns>The <see cref="Model.Tenant"/>.</returns>
        public static Model.Tenant? GetTenant(this HttpContext value)
        {
            return value.Items[TenantKey] as Model.Tenant;
        }

        /// <summary>
        /// Sets the tenant data.
        /// </summary>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <param name="value">A reference to the <see cref="Model.Tenant"/>.</param>
        public static void SetTenant(this HttpContext context, Model.Tenant value)
        {
            context.Items[TenantKey] = value;
            context.Items[TenantIdKey] = value.Id;
        }
    }
}
