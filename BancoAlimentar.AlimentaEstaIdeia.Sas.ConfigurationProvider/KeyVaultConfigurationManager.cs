﻿// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfigurationManager.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Extensions.AspNetCore.Configuration.Secrets;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// This class loads and managed the configuration from Azure Key Vault for each Tenant.
    /// </summary>
    public class KeyVaultConfigurationManager
    {
        private static Dictionary<int, SecretClient> tenantSecretClient = new Dictionary<int, SecretClient>();
        private static Dictionary<int, Dictionary<string, string>> tenantSecretValue = new Dictionary<int, Dictionary<string, string>>();
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

        /// <summary>
        /// Loads all the configuration from the Azure Key Vaults.
        /// </summary>
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
                            TokenCredential credential = new DefaultAzureCredential();
                            credential = new AzureCliCredential(new AzureCliCredentialOptions() { TenantId = "65004861-f3b7-448e-aa2c-6485af17f703" });
                            SecretClient client = new SecretClient(vaultUri: new Uri($"https://{configurationItem.Vault}.vault.azure.net/"), credential: credential);
                            tenantSecretClient.Add(tenant.Id, client);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ensure that the secrets for the tenant are loaded in runtime.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task EnsureTenantConfigurationLoaded(int tenantId)
        {
            if (!tenantSecretValue.ContainsKey(tenantId) && tenantSecretClient.ContainsKey(tenantId))
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

                tenantSecretValue.Add(tenantId, secrets);
            }
        }

        /// <summary>
        /// Gets the specific tenant configuration that has more priority that normal configuration.
        /// </summary>
        /// <param name="tenantId">Tenant ID.</param>
        /// <returns>A dictionary with the configuration for the tenant.</returns>
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