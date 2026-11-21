// -----------------------------------------------------------------------
// <copyright file="IKeyVaultConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using System.Threading;

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
        /// <returns>
        /// True when this call populates the cache; false when already cached or loading fails.
        /// Use <see cref="GetTenantConfiguration"/> and diagnostics to check configuration readiness.
        /// This result does not indicate whether database initialization is needed.
        /// </returns>
        Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions);

        /// <summary>Loads tenant secrets asynchronously with cancellation and a bounded deadline.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="developmentOptions">Development overrides.</param>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>Whether this call populated the cache.</returns>
        Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the specific tenant configuration that has more priority that normal configuration.
        /// </summary>
        /// <param name="tenantId">Tenant ID.</param>
        /// <returns>A dictionary with the configuration for the tenant.</returns>
        Dictionary<string, string>? GetTenantConfiguration(int tenantId);

        /// <summary>
        /// Loads all the configuration from the Azure Key Vaults.
        /// </summary>
        /// <returns>True if the configuration is loaded, false otherwise.</returns>
        bool LoadTenantConfiguration();

        /// <summary>Loads shared metadata asynchronously without blocking request threads.</summary>
        /// <param name="cancellationToken">Caller cancellation.</param>
        /// <returns>Whether shared configuration is available; individual tenant failures are isolated.</returns>
        Task<bool> LoadTenantConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets diagnostics for the most recent failed <see cref="LoadTenantConfiguration"/> call.
        /// </summary>
        /// <returns>Diagnostics when the last load failed; otherwise null.</returns>
        KeyVaultConfigurationLoadDiagnostics? GetLastLoadDiagnostics();
    }
}