// -----------------------------------------------------------------------
// <copyright file="KeyVaultServicePrincipalSettings.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Parsed service principal settings used to access a tenant Key Vault.
    /// </summary>
    public class KeyVaultServicePrincipalSettings
    {
        /// <summary>
        /// Gets or sets the Azure AD tenant id.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the application (client) id.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret value.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Parses a semicolon-delimited service principal connection string.
        /// </summary>
        /// <param name="connectionString">Connection string from SAS core Key Vault.</param>
        /// <returns>Parsed settings.</returns>
        public static KeyVaultServicePrincipalSettings Parse(string connectionString)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return new KeyVaultServicePrincipalSettings();
            }

            foreach (string segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int separatorIndex = segment.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = segment.Substring(0, separatorIndex).Trim();
                string value = segment.Substring(separatorIndex + 1);
                values[key] = value;
            }

            values.TryGetValue("TenantId", out string tenantId);
            values.TryGetValue("ClientId", out string clientId);
            values.TryGetValue("Secret", out string clientSecret);

            return new KeyVaultServicePrincipalSettings
            {
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret,
            };
        }

        /// <summary>
        /// Detects when a client secret looks like a secret identifier instead of the secret value.
        /// </summary>
        /// <param name="clientSecret">Client secret from configuration.</param>
        /// <returns>True when the value resembles an Azure secret id.</returns>
        public static bool LooksLikeSecretIdentifier(string clientSecret)
        {
            return Guid.TryParse(clientSecret, out _);
        }
    }
}
