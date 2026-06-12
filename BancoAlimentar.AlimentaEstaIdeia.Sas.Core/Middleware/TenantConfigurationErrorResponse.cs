// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationErrorResponse.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware
{
    using System;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Formats tenant configuration errors for HTTP responses.
    /// </summary>
    internal static class TenantConfigurationErrorResponse
    {
        /// <summary>
        /// Builds a response body for Key Vault bootstrap failures.
        /// </summary>
        /// <param name="environment">Web host environment.</param>
        /// <param name="diagnostics">Load diagnostics.</param>
        /// <returns>Plain-text response body.</returns>
        public static string BuildKeyVaultLoadFailure(IWebHostEnvironment environment, KeyVaultConfigurationLoadDiagnostics? diagnostics)
        {
            StringBuilder response = new StringBuilder();
            response.AppendLine("Failed to load tenant configuration from Azure Key Vault.");
            response.AppendLine("If you are running locally, make sure to do az login first.");
            response.AppendLine();
            response.AppendLine($"Environment: {environment.EnvironmentName}");
            response.AppendLine($"Host: {environment.ApplicationName}");

            if (diagnostics != null)
            {
                response.AppendLine($"Stage: {diagnostics.Stage}");
                response.AppendLine($"Summary: {diagnostics.Message}");
                response.AppendLine($"SAS core vault: {diagnostics.SasCoreVaultUri}");
                response.AppendLine($"Credential mode: {diagnostics.CredentialMode}");

                if (!string.IsNullOrWhiteSpace(diagnostics.TenantName))
                {
                    response.AppendLine($"Tenant: {diagnostics.TenantName}");
                }

                if (!string.IsNullOrWhiteSpace(diagnostics.TenantVaultUri))
                {
                    response.AppendLine($"Tenant vault: {diagnostics.TenantVaultUri}");
                }

                if (!string.IsNullOrWhiteSpace(diagnostics.ServicePrincipalSecretKeyName))
                {
                    response.AppendLine($"SAS core secret key: {diagnostics.ServicePrincipalSecretKeyName}");
                }

                if (!string.IsNullOrWhiteSpace(diagnostics.AzureAdClientId))
                {
                    response.AppendLine($"Azure AD app (client) id: {diagnostics.AzureAdClientId}");
                }

                if (!string.IsNullOrWhiteSpace(diagnostics.RemediationSteps))
                {
                    response.AppendLine($"Fix: {diagnostics.RemediationSteps}");
                }

                bool showExceptionDetails = environment.IsDevelopment() ||
                    environment.IsStaging() ||
                    string.Equals(diagnostics.Stage, "TenantServicePrincipalAuthenticationFailed", StringComparison.Ordinal) ||
                    string.Equals(diagnostics.Stage, "TenantServicePrincipalInvalidSecret", StringComparison.Ordinal);

                if (showExceptionDetails)
                {
                    if (!string.IsNullOrWhiteSpace(diagnostics.ExceptionType))
                    {
                        response.AppendLine($"Exception: {diagnostics.ExceptionType}");
                    }

                    if (!string.IsNullOrWhiteSpace(diagnostics.ExceptionMessage))
                    {
                        response.AppendLine($"Details: {diagnostics.ExceptionMessage}");
                    }
                }
            }

            response.AppendLine();
            response.AppendLine("What to check:");
            response.AppendLine("1. App Service managed identity has Key Vault access on doar-sas-core and the tenant vault.");
            response.AppendLine("2. ConnectionStrings:Infrastructure points to the infrastructure database for this slot.");
            response.AppendLine("3. KeyVaultConfigurations row exists for this environment with the correct vault name.");
            response.AppendLine("4. If HasServicePrincipalEnabled=1, update the SAS core secret with TenantId=...;ClientId=...;Secret=<secret VALUE> (not the secret id).");
            response.AppendLine("5. Domain mapping exists for this hostname in the infrastructure database.");
            response.AppendLine();
            response.AppendLine("Application Insights:");
            response.AppendLine(diagnostics?.ApplicationInsightsHints ??
                "Search exceptions and traces for 'Error loading SAS Core Key Vault configuration' or 'TenantKeyVaultInitialization'.");

            return response.ToString();
        }
    }
}
