// -----------------------------------------------------------------------
// <copyright file="IKeyVaultConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;

    /// <summary>
    /// Loads and managed the configuration from Azure Key Vault for each Tenant.
    /// </summary>
    public interface IKeyVaultConfigurationManager
    {
        /// <summary>
        /// Ensure that the secrets for the tenant are loaded in runtime.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <param name="developmentOptions">Tenant development options.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions);

        /// <summary>
        /// Gets the specific tenant configuration that has more priority that normal configuration.
        /// </summary>
        /// <param name="tenantId">Tenant ID.</param>
        /// <returns>A dictionary with the configuration for the tenant.</returns>
        Dictionary<string, string>? GetTenantConfiguration(int tenantId);

        /// <summary>
        /// Loads all the configuration from the Azure Key Vaults.
        /// </summary>
        void LoadTenantConfiguration();
    }
}