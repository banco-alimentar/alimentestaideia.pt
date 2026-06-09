// -----------------------------------------------------------------------
// <copyright file="KeyVaultAuthenticationFailureHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Text.RegularExpressions;
    using Azure.Identity;

    /// <summary>
    /// Helpers for Azure AD / Key Vault authentication failures.
    /// </summary>
    public static class KeyVaultAuthenticationFailureHelper
    {
        private static readonly Regex AppIdRegex = new Regex(
            @"app\s+'(?<clientId>[0-9a-fA-F\-]{36})'",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Determines whether an exception represents an Azure authentication failure.
        /// </summary>
        /// <param name="exception">Exception to inspect.</param>
        /// <returns>True for authentication failures.</returns>
        public static bool IsAuthenticationFailure(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is AuthenticationFailedException)
                {
                    return true;
                }

                string message = current.Message ?? string.Empty;
                if (message.Contains("invalid_client", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("AADSTS7000215", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("OAuth token endpoint failure", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds diagnostics for a tenant service principal authentication failure.
        /// </summary>
        /// <param name="exception">Root exception.</param>
        /// <param name="environmentName">ASP.NET Core environment.</param>
        /// <param name="tenantName">Tenant name.</param>
        /// <param name="tenantVaultUri">Tenant vault URI.</param>
        /// <param name="servicePrincipalSecretKeyName">SAS core secret key name.</param>
        /// <param name="servicePrincipalSettings">Parsed service principal settings.</param>
        /// <returns>Diagnostics payload.</returns>
        public static KeyVaultConfigurationLoadDiagnostics BuildServicePrincipalDiagnostics(
            Exception exception,
            string environmentName,
            string tenantName,
            string tenantVaultUri,
            string servicePrincipalSecretKeyName,
            KeyVaultServicePrincipalSettings servicePrincipalSettings)
        {
            string exceptionMessage = GetFullMessage(exception);
            bool invalidClientSecret = exceptionMessage.Contains("AADSTS7000215", StringComparison.OrdinalIgnoreCase) ||
                exceptionMessage.Contains("Invalid client secret provided", StringComparison.OrdinalIgnoreCase);
            bool secretLooksLikeId = KeyVaultServicePrincipalSettings.LooksLikeSecretIdentifier(servicePrincipalSettings?.ClientSecret);
            string clientId = servicePrincipalSettings?.ClientId ?? TryExtractClientId(exceptionMessage);

            string summary = invalidClientSecret
                ? "Azure AD rejected the service principal client secret while loading the tenant Key Vault."
                : "Azure AD authentication failed while loading the tenant Key Vault.";

            string remediation = invalidClientSecret || secretLooksLikeId
                ? "Update the service principal secret in doar-sas-core Key Vault. Store the secret VALUE (shown only when created), not the secret ID. Format: TenantId=...;ClientId=...;Secret=..."
                : "Verify the service principal secret in doar-sas-core Key Vault is current and the app registration still exists.";

            if (secretLooksLikeId)
            {
                remediation += " The configured Secret value looks like a GUID (secret id), not a secret value.";
            }

            return new KeyVaultConfigurationLoadDiagnostics
            {
                Stage = "TenantServicePrincipalAuthenticationFailed",
                Message = summary,
                EnvironmentName = environmentName,
                SasCoreVaultUri = "https://doar-sas-core.vault.azure.net/",
                CredentialMode = "ClientSecretCredential",
                TenantName = tenantName,
                TenantVaultUri = tenantVaultUri,
                ServicePrincipalSecretKeyName = servicePrincipalSecretKeyName,
                AzureAdClientId = clientId,
                RemediationSteps = remediation,
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exceptionMessage,
                ApplicationInsightsHints =
                    "Search for 'TenantServicePrincipalAuthenticationFailed', 'AADSTS7000215', or 'invalid_client'. " +
                    $"Check SAS core secret '{servicePrincipalSecretKeyName}' for app '{clientId}'.",
            };
        }

        private static string GetFullMessage(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            if (exception.InnerException == null)
            {
                return exception.Message;
            }

            return exception.Message + " " + exception.InnerException.Message;
        }

        private static string TryExtractClientId(string message)
        {
            Match match = AppIdRegex.Match(message ?? string.Empty);
            return match.Success ? match.Groups["clientId"].Value : null;
        }
    }
}
