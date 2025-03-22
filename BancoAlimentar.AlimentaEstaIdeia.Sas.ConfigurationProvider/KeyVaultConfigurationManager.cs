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

                    Task<bool> loadKeyVaultConfiguration = this.LoadSasKeyVaultConfiguration();
                    loadKeyVaultConfiguration.Wait();
                    result = loadKeyVaultConfiguration.Result;

                    if (!result)
                    {
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
                                        credential = this.GetServicePrincipalCredential(
                                            this.sasCoreSecrets[configurationItem.SasSPKeyVaultKeyName]);
                                    }
                                    else
                                    {
                                        this.telemetryClient.TrackEvent(
                                            "ServicePrincipal-Secret-NotFound",
                                            new Dictionary<string, string>()
                                            {
                                            { "EnvironmentName", this.environment.EnvironmentName },
                                            { "TenantId", tenant.PublicId.ToString() },
                                            });
                                    }
                                }

                                SecretClient client = new SecretClient(
                                    vaultUri: new Uri($"https://{configurationItem.Vault}.vault.azure.net/"),
                                    credential: credential);
                                tenantSecretClient.AddOrUpdate(tenant.Id, client, (int key, SecretClient secret) =>
                                {
                                    return client;
                                });
                            }
                        }
                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    this.telemetryClient.TrackException(ex);
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

        private async Task<bool> LoadSasKeyVaultConfiguration()
        {
            bool result = false;
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

            KeyVaultSecretManager secretManager = new KeyVaultSecretManager();
            SecretClient secretClient = new SecretClient(vaultUri: new Uri($"https://{SasCoreKeyVaultName}.vault.azure.net/"), credential: credential);
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
                this.logger.LogError(ex, "Error loading SAS Core Key Vault configuration");
                this.telemetryClient.TrackException(ex);
            }

            return result;
        }

        private TokenCredential GetServicePrincipalCredential(string connectionString)
        {
            string tenantId = this.GetValueByName(connectionString, "TenantId");
            string clientId = this.GetValueByName(connectionString, "ClientId");
            string clientSecret = this.GetValueByName(connectionString, "Secret");
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret,
                new ClientSecretCredentialOptions()
                {
                    AdditionallyAllowedTenants = { "*" },
                });
            return clientSecretCredential;
        }

        private string GetValueByName(string connectionString, string key)
        {
            string result = string.Empty;
            string[] items = connectionString.Split(";");
            foreach (var item in items)
            {
                string[] values = item.Split("=");
                if (values[0] == key)
                {
                    result = values[1];
                }
            }

            return result;
        }
    }
}
