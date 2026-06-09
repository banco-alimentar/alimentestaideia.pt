// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Extensions.AspNetCore.Configuration.Secrets;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// This class loads and managed the configuration from Azure Key Vault for each Tenant.
    /// </summary>
    public class KeyVaultConfigurationManager : IKeyVaultConfigurationManager
    {
        private const string SasCoreKeyVaultName = "doar-sas-core";
        private static KeyVaultConfigurationLoadDiagnostics? lastLoadDiagnostics;
        private static ConcurrentDictionary<int, SecretClient> tenantSecretClient = new ConcurrentDictionary<int, SecretClient>();
        private static ConcurrentDictionary<int, Dictionary<string, string>> tenantSecretValue = new ConcurrentDictionary<int, Dictionary<string, string>>();
        private static ReaderWriterLockSlim rwls = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly InfrastructureDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly TelemetryClient telemetryClient;
        private readonly IMemoryCache memoryCache;
        private readonly IConfiguration configuration;
        private readonly ILogger<KeyVaultConfigurationManager> logger;
        private readonly Dictionary<string, string> sasCoreSecrets = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfigurationManager"/> class.
        /// </summary>
        /// <param name="context">A reference to the infrastructure context.</param>
        /// <param name="environment">Web host environment.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="memoryCache">Distributed cache.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger.</param>
        public KeyVaultConfigurationManager(
            InfrastructureDbContext context,
            IWebHostEnvironment environment,
            TelemetryClient telemetryClient,
            IMemoryCache memoryCache,
            IConfiguration configuration,
            ILogger<KeyVaultConfigurationManager> logger)
        {
            this.context = context;
            this.environment = environment;
            this.telemetryClient = telemetryClient;
            this.memoryCache = memoryCache;
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public KeyVaultConfigurationLoadDiagnostics? GetLastLoadDiagnostics()
        {
            return lastLoadDiagnostics;
        }

        /// <inheritdoc/>
        public bool LoadTenantConfiguration()
        {
            bool result = false;
            if (tenantSecretClient.Count == 0)
            {
                Monitor.Enter(this);

                try
                {
                    if (tenantSecretClient.Count != 0)
                    {
                        return false;
                    }

                    lastLoadDiagnostics = null;
                    string credentialMode = this.environment.IsDevelopment() ? "AzureCliCredential" : "ManagedIdentityCredential";
                    Task<bool> loadKeyVaultConfiguration = this.LoadSasKeyVaultConfiguration(credentialMode);
                    loadKeyVaultConfiguration.Wait();
                    result = loadKeyVaultConfiguration.Result;

                    if (!result)
                    {
                        if (lastLoadDiagnostics == null)
                        {
                            this.SetLoadDiagnostics(
                                "SasCoreKeyVaultLoadFailed",
                                $"Unable to read secrets from the SAS core Key Vault '{SasCoreKeyVaultName}'.",
                                credentialMode,
                                null,
                                null,
                                null);
                        }

                        return false;
                    }

                    List<Tenant> allTenants = this.context.Tenants
                        .Include(p => p.KeyVaultConfigurations)
                        .ToList();
                    foreach (Tenant tenant in allTenants)
                    {
                        foreach (KeyVaultConfiguration configurationItem in tenant.KeyVaultConfigurations)
                        {
                            if (this.environment.EnvironmentName == configurationItem.Environment)
                            {
                                TokenCredential credential = new ManagedIdentityCredential();
                                string tenantCredentialMode = credentialMode;
                                if (this.environment.IsDevelopment())
                                {
                                    // this tenant id is for the Banco Alimentar and it to force when
                                    // you are logged in a different tentan by default, for example
                                    // Microsoft's one.
                                    credential = new AzureCliCredential(
                                        new AzureCliCredentialOptions()
                                        {
                                            TenantId = "65004861-f3b7-448e-aa2c-6485af17f703",
                                            AdditionallyAllowedTenants = { "*" },
                                        });
                                }

                                if (configurationItem.HasServicePrincipalEnabled)
                                {
                                    if (this.sasCoreSecrets.ContainsKey(configurationItem.SasSPKeyVaultKeyName))
                                    {
                                        string servicePrincipalConnectionString =
                                            this.sasCoreSecrets[configurationItem.SasSPKeyVaultKeyName];
                                        KeyVaultServicePrincipalSettings servicePrincipalSettings =
                                            KeyVaultServicePrincipalSettings.Parse(servicePrincipalConnectionString);
                                        if (string.IsNullOrWhiteSpace(servicePrincipalSettings.ClientId) ||
                                            string.IsNullOrWhiteSpace(servicePrincipalSettings.ClientSecret) ||
                                            KeyVaultServicePrincipalSettings.LooksLikeSecretIdentifier(servicePrincipalSettings.ClientSecret))
                                        {
                                            lastLoadDiagnostics = KeyVaultAuthenticationFailureHelper.BuildServicePrincipalDiagnostics(
                                                new InvalidOperationException(
                                                    "The service principal secret in SAS core Key Vault is missing ClientId/Secret or the Secret value looks like a secret id (GUID) instead of the secret value."),
                                                this.environment.EnvironmentName,
                                                tenant.Name,
                                                ResolveTenantVaultUri(configurationItem.Vault)?.ToString(),
                                                configurationItem.SasSPKeyVaultKeyName,
                                                servicePrincipalSettings);
                                            lastLoadDiagnostics.Stage = "TenantServicePrincipalInvalidSecret";
                                            return false;
                                        }

                                        credential = this.GetServicePrincipalCredential(servicePrincipalSettings);
                                        tenantCredentialMode = "ClientSecretCredential";
                                    }
                                    else
                                    {
                                        this.telemetryClient.TrackEvent(
                                            "ServicePrincipal-Secret-NotFound",
                                            new Dictionary<string, string>()
                                            {
                                            { "EnvironmentName", this.environment.EnvironmentName },
                                            { "TenantId", tenant.PublicId.ToString() },
                                            { "SasSPKeyVaultKeyName", configurationItem.SasSPKeyVaultKeyName ?? string.Empty },
                                            });
                                        this.SetLoadDiagnostics(
                                            "TenantServicePrincipalSecretMissing",
                                            $"Service principal secret '{configurationItem.SasSPKeyVaultKeyName}' was not found in SAS core Key Vault '{SasCoreKeyVaultName}'.",
                                            tenantCredentialMode,
                                            tenant.Name,
                                            ResolveTenantVaultUri(configurationItem.Vault)?.ToString(),
                                            null);
                                        return false;
                                    }
                                }

                                Uri tenantVaultUri = ResolveTenantVaultUri(configurationItem.Vault);
                                SecretClient client = new SecretClient(
                                    vaultUri: tenantVaultUri,
                                    credential: credential);
                                tenantSecretClient.AddOrUpdate(tenant.Id, client, (int key, SecretClient secret) =>
                                {
                                    return client;
                                });
                            }
                        }
                    }

                    result = true;
                    lastLoadDiagnostics = null;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        ex,
                        "Failed to initialize tenant Key Vault clients for environment {EnvironmentName}",
                        this.environment.EnvironmentName);
                    this.telemetryClient.TrackException(ex, new Dictionary<string, string>()
                    {
                        { "EnvironmentName", this.environment.EnvironmentName },
                        { "Stage", "TenantKeyVaultInitialization" },
                    });
                    this.SetLoadDiagnostics(
                        "TenantKeyVaultInitializationFailed",
                        "An unexpected error occurred while reading tenant metadata or creating Key Vault clients.",
                        this.environment.IsDevelopment() ? "AzureCliCredential" : "ManagedIdentityCredential",
                        null,
                        null,
                        ex);
                    result = false;
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions)
        {
            bool result = false;
            rwls.EnterReadLock();
            bool needUpdate = false;
            try
            {
                if (developmentOptions == TenantDevelopmentOptions.ProductionOptions)
                {
                    // check if we can find
                    if (!tenantSecretValue.ContainsKey(tenantId) && tenantSecretClient.ContainsKey(tenantId))
                    {
                        result = true;
                        needUpdate = true;
                    }
                }
                else
                {
                    if (!tenantSecretValue.ContainsKey(tenantId))
                    {
                        needUpdate = true;
                    }
                }
            }
            finally
            {
                rwls.ExitReadLock();
            }

            if (needUpdate)
            {
                string cacheKeyName = $"TenantSecret-{tenantId}";
                Dictionary<string, string> secrets = this.memoryCache.Get<Dictionary<string, string>>(cacheKeyName);
                if (developmentOptions == TenantDevelopmentOptions.ProductionOptions || !developmentOptions.UseSecrets)
                {
                    if (secrets == null || secrets?.Count == 0)
                    {
                        if (secrets == null)
                        {
                            secrets = new Dictionary<string, string>();
                        }

                        KeyVaultSecretManager secretManager = new KeyVaultSecretManager();
                        if (tenantSecretClient.ContainsKey(tenantId))
                        {
                            try
                            {
                                SecretClient secretClient = tenantSecretClient[tenantId];
                                AsyncPageable<SecretProperties> page = secretClient.GetPropertiesOfSecretsAsync();
                                await foreach (SecretProperties secretItem in page)
                                {
                                    Response<KeyVaultSecret> responseSecret = await secretClient.GetSecretAsync(secretItem.Name);
                                    if (responseSecret.Value != null)
                                    {
                                        secrets.Add(
                                            secretManager.GetKey(responseSecret.Value),
                                            responseSecret.Value.Value);
                                    }
                                    else
                                    {
                                        this.telemetryClient.TrackEvent("SecretNotFound");
                                    }
                                }

                                this.memoryCache.Set(cacheKeyName, secrets);

                                result = true;
                            }
                            catch (Exception ex) when (KeyVaultAuthenticationFailureHelper.IsAuthenticationFailure(ex))
                            {
                                Tenant tenant = await this.GetTenantWithKeyVaultConfigurationAsync(tenantId);
                                KeyVaultConfiguration keyVaultConfiguration = tenant?.KeyVaultConfigurations?
                                    .FirstOrDefault(item => item.Environment == this.environment.EnvironmentName);
                                KeyVaultServicePrincipalSettings servicePrincipalSettings = keyVaultConfiguration != null &&
                                    !string.IsNullOrWhiteSpace(keyVaultConfiguration.SasSPKeyVaultKeyName) &&
                                    this.sasCoreSecrets.TryGetValue(keyVaultConfiguration.SasSPKeyVaultKeyName, out string connectionString)
                                    ? KeyVaultServicePrincipalSettings.Parse(connectionString)
                                    : new KeyVaultServicePrincipalSettings();

                                lastLoadDiagnostics = KeyVaultAuthenticationFailureHelper.BuildServicePrincipalDiagnostics(
                                    ex,
                                    this.environment.EnvironmentName,
                                    tenant?.Name,
                                    keyVaultConfiguration != null ? ResolveTenantVaultUri(keyVaultConfiguration.Vault)?.ToString() : null,
                                    keyVaultConfiguration?.SasSPKeyVaultKeyName,
                                    servicePrincipalSettings);

                                this.logger.LogError(
                                    ex,
                                    "Service principal authentication failed for tenant {TenantId}. ClientId={ClientId}. SasCoreSecretKey={SasCoreSecretKey}",
                                    tenantId,
                                    servicePrincipalSettings.ClientId,
                                    keyVaultConfiguration?.SasSPKeyVaultKeyName);

                                this.telemetryClient.TrackException(ex, new Dictionary<string, string>()
                                {
                                    { "Stage", lastLoadDiagnostics.Stage },
                                    { "TenantId", tenantId.ToString() },
                                    { "ClientId", servicePrincipalSettings.ClientId ?? string.Empty },
                                    { "SasSPKeyVaultKeyName", keyVaultConfiguration?.SasSPKeyVaultKeyName ?? string.Empty },
                                });

                                return false;
                            }
                        }
                    }
                }
                else
                {
                    if (secrets == null)
                    {
                        secrets = new Dictionary<string, string>();
                    }

                    IEnumerable<KeyValuePair<string, string>> enumerator = this.configuration.AsEnumerable();
                    foreach (var item in enumerator)
                    {
                        if (!secrets.ContainsKey(item.Key))
                        {
                            secrets.Add(item.Key, item.Value);
                        }
                        else
                        {
                            secrets[item.Key] = item.Value;
                        }
                    }

                    result = true;
                }

                rwls.EnterWriteLock();
                try
                {
                    if (!tenantSecretValue.ContainsKey(tenantId) && secrets != null)
                    {
                        tenantSecretValue.AddOrUpdate(tenantId, secrets, (int key, Dictionary<string, string> existingValue) =>
                        {
                            foreach (var item in existingValue)
                            {
                                if (secrets.ContainsKey(item.Key))
                                {
                                    existingValue[item.Key] = secrets[item.Key];
                                }
                            }

                            return existingValue;
                        });
                    }
                }
                finally
                {
                    rwls.ExitWriteLock();
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public Dictionary<string, string>? GetTenantConfiguration(int tenantId)
        {
            if (tenantSecretValue.ContainsKey(tenantId))
            {
                return tenantSecretValue[tenantId];
            }
            else
            {
                return null;
            }
        }

        private async Task<bool> LoadSasKeyVaultConfiguration(string credentialMode)
        {
            bool result = false;
            Uri sasCoreVaultUri = new Uri($"https://{SasCoreKeyVaultName}.vault.azure.net/");
            TokenCredential credential = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions()
                {
                    AdditionallyAllowedTenants = { "*" },
                });
            if (this.environment.IsDevelopment())
            {
                credential = new AzureCliCredential(new AzureCliCredentialOptions()
                {
                    TenantId = "65004861-f3b7-448e-aa2c-6485af17f703",
                    AdditionallyAllowedTenants = { "*" },
                });
            }
            else
            {
                credentialMode = "DefaultAzureCredential";
            }

            KeyVaultSecretManager secretManager = new KeyVaultSecretManager();
            SecretClient secretClient = new SecretClient(vaultUri: sasCoreVaultUri, credential: credential);
            AsyncPageable<SecretProperties> page = secretClient.GetPropertiesOfSecretsAsync();
            try
            {
                await foreach (SecretProperties secretItem in page)
                {
                    Response<KeyVaultSecret> responseSecret = await secretClient.GetSecretAsync(secretItem.Name);
                    this.sasCoreSecrets.Add(
                        secretManager.GetKey(responseSecret.Value),
                        responseSecret.Value.Value);
                }

                result = true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(
                    ex,
                    "Error loading SAS Core Key Vault configuration from {SasCoreVaultUri} using {CredentialMode}",
                    sasCoreVaultUri,
                    credentialMode);
                this.telemetryClient.TrackException(ex, new Dictionary<string, string>()
                {
                    { "EnvironmentName", this.environment.EnvironmentName },
                    { "SasCoreVaultUri", sasCoreVaultUri.ToString() },
                    { "CredentialMode", credentialMode },
                    { "Stage", "SasCoreKeyVaultLoadFailed" },
                });
                this.SetLoadDiagnostics(
                    "SasCoreKeyVaultLoadFailed",
                    $"Unable to read secrets from SAS core Key Vault '{sasCoreVaultUri}'.",
                    credentialMode,
                    null,
                    sasCoreVaultUri.ToString(),
                    ex);
            }

            return result;
        }

        private void SetLoadDiagnostics(
            string stage,
            string message,
            string credentialMode,
            string tenantName,
            string tenantVaultUri,
            Exception exception)
        {
            lastLoadDiagnostics = new KeyVaultConfigurationLoadDiagnostics
            {
                Stage = stage,
                Message = message,
                EnvironmentName = this.environment.EnvironmentName,
                SasCoreVaultUri = $"https://{SasCoreKeyVaultName}.vault.azure.net/",
                CredentialMode = credentialMode,
                TenantName = tenantName,
                TenantVaultUri = tenantVaultUri,
                ExceptionType = exception?.GetType().Name,
                ExceptionMessage = exception?.Message,
                ApplicationInsightsHints =
                    "Search exceptions and traces for 'Error loading SAS Core Key Vault configuration', " +
                    "'TenantKeyVaultInitialization', 'ServicePrincipal-Secret-NotFound', or stage '" + stage + "'.",
            };
        }

        private static Uri ResolveTenantVaultUri(Uri vault)
        {
            if (vault == null)
            {
                throw new InvalidOperationException("Tenant Key Vault configuration is missing a vault URI.");
            }

            if (vault.IsAbsoluteUri && vault.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase))
            {
                return vault;
            }

            string vaultName = vault.IsAbsoluteUri
                ? vault.Host
                : vault.ToString().Trim().Trim('/');
            return new Uri($"https://{vaultName}.vault.azure.net/");
        }

        private async Task<Tenant> GetTenantWithKeyVaultConfigurationAsync(int tenantId)
        {
            return await this.context.Tenants
                .Include(item => item.KeyVaultConfigurations)
                .FirstOrDefaultAsync(item => item.Id == tenantId);
        }

        private TokenCredential GetServicePrincipalCredential(KeyVaultServicePrincipalSettings settings)
        {
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(
                settings.TenantId,
                settings.ClientId,
                settings.ClientSecret,
                new ClientSecretCredentialOptions()
                {
                    AdditionallyAllowedTenants = { "*" },
                });
            return clientSecretCredential;
        }
    }
}
