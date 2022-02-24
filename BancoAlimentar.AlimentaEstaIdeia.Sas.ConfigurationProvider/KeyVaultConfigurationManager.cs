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
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Extensions.AspNetCore.Configuration.Secrets;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// This class loads and managed the configuration from Azure Key Vault for each Tenant.
    /// </summary>
    public class KeyVaultConfigurationManager : IKeyVaultConfigurationManager
    {
        private static Dictionary<int, SecretClient> tenantSecretClient = new Dictionary<int, SecretClient>();
        private static Dictionary<int, Dictionary<string, string>> tenantSecretValue = new Dictionary<int, Dictionary<string, string>>();
        private static ReaderWriterLockSlim rwls = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly InfrastructureDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultConfigurationManager"/> class.
        /// </summary>
        /// <param name="context">A reference to the infrastructure context.</param>
        /// <param name="environment">Web host environment.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public KeyVaultConfigurationManager(
            InfrastructureDbContext context,
            IWebHostEnvironment environment,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.environment = environment;
            this.telemetryClient = telemetryClient;
            this.LoadTenantConfiguration();
        }

        /// <inheritdoc/>
        public void LoadTenantConfiguration()
        {
            if (tenantSecretClient.Count == 0)
            {
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
                                credential = new AzureCliCredential(new AzureCliCredentialOptions() { TenantId = "65004861-f3b7-448e-aa2c-6485af17f703" });
                            }

                            SecretClient client = new SecretClient(vaultUri: new Uri($"https://{configurationItem.Vault}.vault.azure.net/"), credential: credential);
                            tenantSecretClient.Add(tenant.Id, client);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> EnsureTenantConfigurationLoaded(int tenantId)
        {
            bool result = false;
            rwls.EnterReadLock();
            bool needUpdate = false;
            try
            {
                if (!tenantSecretValue.ContainsKey(tenantId) && tenantSecretClient.ContainsKey(tenantId))
                {
                    needUpdate = true;
                }
                else
                {
                    this.telemetryClient.TrackEvent(
                        "TenantConfigurationNotFound",
                        new Dictionary<string, string>()
                        {
                        { "tenantSecretValue.ContainsKey(tenantId)", tenantSecretValue.ContainsKey(tenantId).ToString() },
                        { "tenantSecretClient.ContainsKey(tenantId)", tenantSecretClient.ContainsKey(tenantId).ToString() },
                        });
                }
            }
            finally
            {
                rwls.ExitReadLock();
            }

            if (needUpdate)
            {
                Dictionary<string, string> secrets = new Dictionary<string, string>();
                KeyVaultSecretManager secretManager = new KeyVaultSecretManager();
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

                result = true;
                rwls.EnterWriteLock();
                try
                {
                    if (!tenantSecretValue.ContainsKey(tenantId))
                    {
                        tenantSecretValue.Add(tenantId, secrets);
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
    }
}
