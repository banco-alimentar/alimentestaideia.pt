// -----------------------------------------------------------------------
// <copyright file="HttpContextExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core
{
    using System.Diagnostics.CodeAnalysis;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

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

        private const string TenantConfigurationKey = "__TenantConfiguration";

        /// <summary>
        /// Gets the tenant data.
        /// </summary>
        /// <param name="value">A reference to the <see cref="HttpContext"/>.</param>
        /// <returns>The <see cref="Model.Tenant"/>.</returns>
        public static Model.Tenant GetTenant(this HttpContext value)
        {
            Model.Tenant? tenant = value.Features.Get<Model.Tenant>();
            return tenant != null ? tenant : Model.Tenant.EmptyTenant;
        }

        /// <summary>
        /// Sets the tenant data.
        /// </summary>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <param name="value">A reference to the <see cref="Model.Tenant"/>.</param>
        public static void SetTenant(this HttpContext context, Model.Tenant value)
        {
            context.Features.Set(value);
        }

        /// <summary>
        /// Gets the current tenant configuration.
        /// </summary>
        /// <remarks>
        /// Returns the snapshot captured by <see cref="CaptureTenantSpecificConfiguration"/> when present, so
        /// a concurrent reload cannot change or remove the configuration of a request that is already running.
        /// </remarks>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="IDictionary{TKey, TValue}"/> with the tenant specific configuration.</returns>
        public static IDictionary<string, string>? GetTenantSpecificConfiguration(this HttpContext context)
        {
            IDictionary<string, string>? result = new Dictionary<string, string>();
            if (context.TryGetCapturedTenantConfiguration(out IDictionary<string, string>? captured))
            {
                return captured;
            }

            Model.Tenant? tenant = context.GetTenant();
            IKeyVaultConfigurationManager? keyVaultConfigurationManager = context.Items[typeof(IKeyVaultConfigurationManager).Name] as IKeyVaultConfigurationManager;
            if (keyVaultConfigurationManager != null && tenant != null)
            {
                result = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
            }

            return result;
        }

        /// <summary>
        /// Gets the tenant configuration pinned to the current request for the current tenant.
        /// </summary>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <param name="configuration">The pinned configuration, when present.</param>
        /// <returns>True when the request has pinned configuration for its tenant.</returns>
        public static bool TryGetCapturedTenantConfiguration(this HttpContext context, [NotNullWhen(true)] out IDictionary<string, string>? configuration)
        {
            configuration = null;
            Model.Tenant? tenant = context.Features.Get<Model.Tenant>();
            if (tenant != null &&
                context.Items.TryGetValue(TenantConfigurationKey, out object? captured) &&
                captured is CapturedTenantConfiguration snapshot &&
                snapshot.TenantId == tenant.Id)
            {
                configuration = snapshot.Values;
            }

            return configuration != null;
        }

        /// <summary>
        /// Pins the verified tenant configuration to the current request.
        /// </summary>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <param name="tenantId">Tenant that owns the configuration.</param>
        /// <param name="configuration">Verified tenant configuration.</param>
        public static void CaptureTenantSpecificConfiguration(this HttpContext context, int tenantId, IDictionary<string, string> configuration)
        {
            context.Items[TenantConfigurationKey] = new CapturedTenantConfiguration(tenantId, configuration);
        }

        /// <summary>
        /// Get the extended tenant properties from the database.
        /// </summary>
        /// <remarks>This is a separate method because we can't resolve this at the same time.</remarks>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="IDictionary{TKey, TValue}"/> with the tenant specific configuration.</returns>
        public static IDictionary<string, string> GetExtendedTenantProperties(this HttpContext context)
        {
            Model.Tenant? tenant = context.GetTenant();

            TenantDatabaseConfigurationInMemoryProvider tenantDatabaseConfigurationInMemoryProvider =
                new TenantDatabaseConfigurationInMemoryProvider(
                    context.RequestServices.GetRequiredService<ApplicationDbContext>(),
                    context,
                    context.RequestServices.GetRequiredService<InMemoryCacheService>());

            return tenantDatabaseConfigurationInMemoryProvider.GetTenantConfiguration();
        }

        private sealed record CapturedTenantConfiguration(int TenantId, IDictionary<string, string> Values);
    }
}
