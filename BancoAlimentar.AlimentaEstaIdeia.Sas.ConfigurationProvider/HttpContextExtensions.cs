// -----------------------------------------------------------------------
// <copyright file="HttpContextExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core
{
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
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="IDictionary{TKey, TValue}"/> with the tenant specific configuration.</returns>
        public static IDictionary<string, string>? GetTenantSpecificConfiguration(this HttpContext context)
        {
            IDictionary<string, string>? result = new Dictionary<string, string>();
            IKeyVaultConfigurationManager? keyVaultConfigurationManager = context.Items[typeof(IKeyVaultConfigurationManager).Name] as IKeyVaultConfigurationManager;
            Model.Tenant? tenant = context.GetTenant();
            if (keyVaultConfigurationManager != null && tenant != null)
            {
                result = keyVaultConfigurationManager.GetTenantConfiguration(tenant.Id);
            }

            return result;
        }

        /// <summary>
        /// Get the extended tenant properties from the database.
        /// </summary>
        /// <remarks>This is a separate method because we can't resolve this at the same time.</remarks>
        /// <param name="context">A reference to the <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="IDictionary{TKey, TValue}"/> with the tenant specific configuration.</returns>
        public static IDictionary<string, string> GetExtendedTenantProperties(this HttpContext context)
        {
            IDictionary<string, string>? result = new Dictionary<string, string>();
            Model.Tenant? tenant = context.GetTenant();
            TenantDatabaseConfigurationInMemoryProvider tenantDatabaseConfigurationInMemoryProvider =
                new TenantDatabaseConfigurationInMemoryProvider(
                    context.RequestServices.GetRequiredService<ApplicationDbContext>(),
                    context,
                    context.RequestServices.GetRequiredService<InMemoryCacheService>());
            
            Dictionary<string, string> tenantDataBaseConfiguration = tenantDatabaseConfigurationInMemoryProvider.GetTenantConfiguration();
            foreach (KeyValuePair<string, string> item in tenantDataBaseConfiguration)
            {
                if (result.ContainsKey(item.Key))
                {
                    result[item.Key] = item.Value;
                }
                else
                {
                    result.Add(item.Key, item.Value);
                }
            }

            return result;
        }
    }
}
