// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This class loads and managed the configuration from Azure Key Vault for each Tenant.
    /// </summary>
    public class KeyVaultConfigurationManager : IKeyVaultConfigurationManager
    {
        private const string SasCoreKeyVaultName = "doar-sas-core";
        private KeyVaultConfigurationLoadDiagnostics? lastLoadDiagnostics;
        private readonly InfrastructureDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly TelemetryClient telemetryClient;
        private readonly KeyVaultConfigurationCache cache;
        private readonly IConfiguration configuration;
        private readonly ILogger<KeyVaultConfigurationManager> logger;
        private readonly TenantRuntimeOptions runtimeOptions;
        private readonly TimeProvider clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfigurationManager"/> class.
        /// </summary>
        /// <param name="context">A reference to the infrastructure context.</param>
        /// <param name="environment">Web host environment.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="cache">Host-wide configuration cache.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="runtimeOptions">Host-level load limits.</param>
        /// <param name="clock">Clock used for deadlines and retries.</param>
        public KeyVaultConfigurationManager(
            InfrastructureDbContext context,
            IWebHostEnvironment environment,
            TelemetryClient telemetryClient,
            KeyVaultConfigurationCache cache,
            IConfiguration configuration,
            ILogger<KeyVaultConfigurationManager> logger,
            IOptions<TenantRuntimeOptions>? runtimeOptions = null,
            TimeProvider? clock = null)
        {
            this.context = context;
            this.environment = environment;
            this.telemetryClient = telemetryClient;
            this.cache = cache;
            this.configuration = configuration;
            this.logger = logger;
            this.runtimeOptions = runtimeOptions?.Value ?? new TenantRuntimeOptions();
            this.clock = clock ?? TimeProvider.System;
            if (!this.runtimeOptions.IsValid())
            {
                throw new ArgumentException("Tenant runtime intervals must be positive and bounded.", nameof(runtimeOptions));
            }
        }

        /// <inheritdoc/>
        public KeyVaultConfigurationLoadDiagnostics? GetLastLoadDiagnostics()
        {
            return lastLoadDiagnostics;
        }

        /// <inheritdoc/>
        public bool LoadTenantConfiguration()
        {
            return this.LoadTenantConfigurationAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task<bool> LoadTenantConfigurationAsync(CancellationToken cancellationToken = default)
        {
            KeyVaultConfigurationCache.State state = this.cache.Current;
            using var deadline = new CancellationTokenSource(this.runtimeOptions.ConfigurationLoadTimeout, this.clock);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            bool acquired = false;
            try
            {
                await state.BootstrapLock.WaitAsync(linked.Token);
                acquired = true;
                if (state.Initialized)
                {
                    this.lastLoadDiagnostics = null;
                    return true;
                }

                if (state.BootstrapFailure is { } previous && previous.RetryAt > this.clock.GetUtcNow())
                {
                    this.lastLoadDiagnostics = previous.Diagnostics;
                    return false;
                }

                this.lastLoadDiagnostics = null;
                var coreSecrets = new Dictionary<string, string>();
                string credentialMode = this.environment.IsDevelopment() ? "AzureCliCredential" : "ManagedIdentityCredential";
                if (!await this.LoadSasKeyVaultConfiguration(credentialMode, coreSecrets, linked.Token))
                {
                    if (this.lastLoadDiagnostics == null)
                    {
                        this.SetLoadDiagnostics(
                            "SasCoreKeyVaultLoadFailed",
                            $"Unable to read secrets from the SAS core Key Vault '{SasCoreKeyVaultName}'.",
                            credentialMode,
                            null,
                            null,
                            null);
                    }

                    this.RecordFailure(state, null);
                    return false;
                }

                List<Tenant> allTenants = await this.context.Tenants
                    .AsNoTracking()
                    .Include(p => p.KeyVaultConfigurations)
                    .ToListAsync(linked.Token);
                state.CoreSecrets = coreSecrets;
                foreach (Tenant tenant in allTenants)
                {
                    state.Tenants[tenant.Id] = tenant;
                    this.TryInitializeTenant(state, tenant);
                }

                state.Initialized = true;
                state.BootstrapFailure = null;
                this.lastLoadDiagnostics = null;
                return true;
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                this.SetTimeoutDiagnostics("ConfigurationBootstrapTimedOut");
                if (acquired)
                {
                    this.RecordFailure(state, null);
                }

                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
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
                this.RecordFailure(state, null);
                return false;
            }
            finally
            {
                if (acquired)
                {
                    state.BootstrapLock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions)
        {
            return await this.EnsureTenantConfigurationLoaded(tenantId, developmentOptions, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions, CancellationToken cancellationToken)
        {
            KeyVaultConfigurationCache.State state = this.cache.Current;
            SemaphoreSlim tenantLock = state.TenantLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
            using var deadline = new CancellationTokenSource(this.runtimeOptions.ConfigurationLoadTimeout, this.clock);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            bool acquired = false;
            try
            {
                await tenantLock.WaitAsync(linked.Token);
                acquired = true;
                this.lastLoadDiagnostics = null;
                if (state.Values.ContainsKey(tenantId))
                {
                    return false;
                }

                if (state.Failures.TryGetValue(tenantId, out KeyVaultConfigurationCache.Failure? previous)
                    && previous.RetryAt > this.clock.GetUtcNow())
                {
                    this.lastLoadDiagnostics = previous.Diagnostics;
                    return false;
                }

                var secrets = new Dictionary<string, string>();
                if (developmentOptions == TenantDevelopmentOptions.ProductionOptions || !developmentOptions.UseSecrets)
                {
                    if (!state.Clients.TryGetValue(tenantId, out SecretClient? client))
                    {
                        if (state.Tenants.TryGetValue(tenantId, out Tenant? tenant))
                        {
                            if (!this.TryInitializeTenant(state, tenant))
                            {
                                return false;
                            }

                            state.Clients.TryGetValue(tenantId, out client);
                        }
                    }

                    if (client == null)
                    {
                        this.SetLoadDiagnostics(
                            "TenantKeyVaultClientMissing",
                            $"No Key Vault client is configured for tenant {tenantId}.",
                            this.environment.IsDevelopment() ? "AzureCliCredential" : "ManagedIdentityCredential",
                            null,
                            null,
                            null);
                        this.logger.LogError("No Key Vault client is configured for tenant {TenantId}", tenantId);
                        this.RecordFailure(state, tenantId);
                        return false;
                    }

                    if (!await this.TryLoadTenantSecretsAsync(tenantId, client, state, secrets, linked.Token))
                    {
                        this.RecordFailure(state, tenantId);
                        return false;
                    }
                }
                else
                {
                    this.ApplyConfigurationOverrides(secrets);
                }

                state.Values[tenantId] = secrets;
                state.LoadedAt[tenantId] = this.clock.GetUtcNow();
                state.Failures.TryRemove(tenantId, out _);
                return true;
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                this.SetTimeoutDiagnostics("TenantConfigurationLoadTimedOut");
                if (acquired)
                {
                    this.RecordFailure(state, tenantId);
                }

                return false;
            }
            finally
            {
                if (acquired)
                {
                    tenantLock.Release();
                }
            }
        }

        private bool TryInitializeTenant(KeyVaultConfigurationCache.State state, Tenant tenant)
        {
            this.lastLoadDiagnostics = null;
            var clients = new Dictionary<int, SecretClient>();
            try
            {
                foreach (KeyVaultConfiguration item in tenant.KeyVaultConfigurations.Where(item => item.Environment == this.environment.EnvironmentName))
                {
                    if (!this.TryInitializeTenantClient(
                        tenant,
                        item,
                        this.environment.IsDevelopment() ? "AzureCliCredential" : "ManagedIdentityCredential",
                        state.CoreSecrets,
                        clients))
                    {
                        this.RecordFailure(state, tenant.Id);
                        this.logger.LogError("Tenant {TenantId} configuration failed at {Stage}", tenant.Id, this.lastLoadDiagnostics?.Stage);
                        return false;
                    }
                }

                if (clients.TryGetValue(tenant.Id, out SecretClient? client))
                {
                    state.Clients[tenant.Id] = client;
                }

                return true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                this.SetLoadDiagnostics("TenantKeyVaultInitializationFailed", "Tenant client configuration is invalid.", null, tenant.Name, null, exception);
                this.logger.LogError(exception, "Failed to configure client for tenant {TenantId}", tenant.Id);
                this.RecordFailure(state, tenant.Id);
                return false;
            }
        }

        private void RecordFailure(KeyVaultConfigurationCache.State state, int? tenantId)
        {
            KeyVaultConfigurationLoadDiagnostics diagnostics = this.lastLoadDiagnostics
                ?? throw new InvalidOperationException("A configuration failure requires diagnostics.");
            KeyVaultConfigurationCache.Failure? previous = state.BootstrapFailure;
            if (tenantId.HasValue)
            {
                state.Failures.TryGetValue(tenantId.Value, out previous);
            }

            int attempts = Math.Min(31, (previous?.Attempts ?? 0) + 1);
            var failure = new KeyVaultConfigurationCache.Failure(diagnostics, attempts, this.clock.GetUtcNow() + this.runtimeOptions.GetRetryDelay(attempts));
            if (tenantId.HasValue)
            {
                state.Failures[tenantId.Value] = failure;
            }
            else
            {
                state.BootstrapFailure = failure;
            }
        }

        private void SetTimeoutDiagnostics(string stage)
        {
            this.SetLoadDiagnostics(stage, "Configuration loading exceeded the configured deadline.", null, null, null, null);
            this.logger.LogError("Tenant configuration load exceeded its deadline at {Stage}", stage);
        }

        /// <inheritdoc/>
        public Dictionary<string, string>? GetTenantConfiguration(int tenantId)
        {
            return this.cache.Current.Values.TryGetValue(tenantId, out Dictionary<string, string>? secrets) ? secrets : null;
        }

        /// <summary>
        /// Creates a client for a configured vault.
        /// </summary>
        /// <param name="vaultUri">Vault URI.</param>
        /// <param name="credential">Credential selected for this vault.</param>
        /// <returns>The vault client.</returns>
        protected virtual SecretClient CreateSecretClient(Uri vaultUri, TokenCredential credential)
        {
            return new SecretClient(vaultUri, credential);
        }

        private bool TryInitializeTenantClient(
            Tenant tenant,
            KeyVaultConfiguration configurationItem,
            string credentialMode,
            Dictionary<string, string> coreSecrets,
            Dictionary<int, SecretClient> clients)
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
                if (!coreSecrets.ContainsKey(configurationItem.SasSPKeyVaultKeyName))
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

                string servicePrincipalConnectionString = coreSecrets[configurationItem.SasSPKeyVaultKeyName];
                KeyVaultServicePrincipalSettings servicePrincipalSettings = KeyVaultServicePrincipalSettings.Parse(servicePrincipalConnectionString);
                if (string.IsNullOrWhiteSpace(servicePrincipalSettings.ClientId) ||
                    string.IsNullOrWhiteSpace(servicePrincipalSettings.ClientSecret) ||
                    KeyVaultServicePrincipalSettings.LooksLikeSecretIdentifier(servicePrincipalSettings.ClientSecret))
                {
                    this.lastLoadDiagnostics = KeyVaultAuthenticationFailureHelper.BuildServicePrincipalDiagnostics(
                        new InvalidOperationException(
                            "The service principal secret in SAS core Key Vault is missing ClientId/Secret or the Secret value looks like a secret id (GUID) instead of the secret value."),
                        this.environment.EnvironmentName,
                        tenant.Name,
                        ResolveTenantVaultUri(configurationItem.Vault)?.ToString(),
                        configurationItem.SasSPKeyVaultKeyName,
                        servicePrincipalSettings);
                    this.lastLoadDiagnostics.Stage = "TenantServicePrincipalInvalidSecret";
                    return false;
                }

                credential = this.GetServicePrincipalCredential(servicePrincipalSettings);
                tenantCredentialMode = "ClientSecretCredential";
            }

            Uri tenantVaultUri = ResolveTenantVaultUri(configurationItem.Vault);
            clients[tenant.Id] = this.CreateSecretClient(tenantVaultUri, credential);
            return true;
        }

        private async Task<bool> TryLoadTenantSecretsAsync(
            int tenantId,
            SecretClient secretClient,
            KeyVaultConfigurationCache.State state,
            Dictionary<string, string> secrets,
            CancellationToken cancellationToken)
        {
            try
            {
                KeyVaultSecretManager secretManager = new KeyVaultSecretManager();
                AsyncPageable<SecretProperties> page = secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
                await foreach (SecretProperties secretItem in page.WithCancellation(cancellationToken))
                {
                    Response<KeyVaultSecret> responseSecret = await secretClient.GetSecretAsync(secretItem.Name, cancellationToken: cancellationToken).WaitAsync(cancellationToken);
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

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (KeyVaultAuthenticationFailureHelper.IsAuthenticationFailure(ex))
                {
                    state.Tenants.TryGetValue(tenantId, out Tenant? tenant);
                    this.RecordTenantAuthenticationFailure(tenantId, tenant, state.CoreSecrets, ex);
                }
                else
                {
                    this.SetLoadDiagnostics("TenantSecretsLoadFailed", "Unable to load tenant secrets.", null, null, null, ex);
                    this.logger.LogError(ex, "Unable to load secrets for tenant {TenantId}", tenantId);
                    this.telemetryClient.TrackException(ex);
                }

                return false;
            }
        }

        private void ApplyConfigurationOverrides(Dictionary<string, string> secrets)
        {
            IConfiguration hostConfiguration = this.configuration is TenantConfigurationRoot tenantConfiguration
                ? tenantConfiguration.HostConfiguration
                : this.configuration;
            IEnumerable<KeyValuePair<string, string>> enumerator = hostConfiguration.AsEnumerable();
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
        }

        private void RecordTenantAuthenticationFailure(int tenantId, Tenant? tenant, Dictionary<string, string> coreSecrets, Exception exception)
        {
            KeyVaultConfiguration keyVaultConfiguration = tenant?.KeyVaultConfigurations?
                .FirstOrDefault(item => item.Environment == this.environment.EnvironmentName);
            KeyVaultServicePrincipalSettings servicePrincipalSettings = keyVaultConfiguration != null &&
                !string.IsNullOrWhiteSpace(keyVaultConfiguration.SasSPKeyVaultKeyName) &&
                coreSecrets.TryGetValue(keyVaultConfiguration.SasSPKeyVaultKeyName, out string connectionString)
                ? KeyVaultServicePrincipalSettings.Parse(connectionString)
                : new KeyVaultServicePrincipalSettings();

            this.lastLoadDiagnostics = KeyVaultAuthenticationFailureHelper.BuildServicePrincipalDiagnostics(
                exception,
                this.environment.EnvironmentName,
                tenant?.Name,
                keyVaultConfiguration != null ? ResolveTenantVaultUri(keyVaultConfiguration.Vault)?.ToString() : null,
                keyVaultConfiguration?.SasSPKeyVaultKeyName,
                servicePrincipalSettings);

            this.logger.LogError(
                exception,
                "Service principal authentication failed for tenant {TenantId}. ClientId={ClientId}. SasCoreSecretKey={SasCoreSecretKey}",
                tenantId,
                servicePrincipalSettings.ClientId,
                keyVaultConfiguration?.SasSPKeyVaultKeyName);

            this.telemetryClient.TrackException(exception, new Dictionary<string, string>()
            {
                { "Stage", this.lastLoadDiagnostics.Stage },
                { "TenantId", tenantId.ToString() },
                { "ClientId", servicePrincipalSettings.ClientId ?? string.Empty },
                { "SasSPKeyVaultKeyName", keyVaultConfiguration?.SasSPKeyVaultKeyName ?? string.Empty },
            });
        }

        private async Task<bool> LoadSasKeyVaultConfiguration(string credentialMode, Dictionary<string, string> coreSecrets, CancellationToken cancellationToken)
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
            SecretClient secretClient = this.CreateSecretClient(sasCoreVaultUri, credential);
            AsyncPageable<SecretProperties> page = secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
            try
            {
                await foreach (SecretProperties secretItem in page.WithCancellation(cancellationToken))
                {
                    Response<KeyVaultSecret> responseSecret = await secretClient.GetSecretAsync(secretItem.Name, cancellationToken: cancellationToken).WaitAsync(cancellationToken);
                    coreSecrets.Add(
                        secretManager.GetKey(responseSecret.Value),
                        responseSecret.Value.Value);
                }

                result = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
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
            this.lastLoadDiagnostics = new KeyVaultConfigurationLoadDiagnostics
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
