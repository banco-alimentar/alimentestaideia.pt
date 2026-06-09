// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfigurationLoadDiagnostics.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    /// <summary>
    /// Describes why tenant Key Vault configuration failed to load.
    /// </summary>
    public class KeyVaultConfigurationLoadDiagnostics
    {
        /// <summary>
        /// Gets or sets the failure stage identifier.
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Gets or sets the human-readable failure summary.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the ASP.NET Core environment name.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Gets or sets the SAS core Key Vault URI that was contacted.
        /// </summary>
        public string SasCoreVaultUri { get; set; }

        /// <summary>
        /// Gets or sets the credential type used to access Key Vault.
        /// </summary>
        public string CredentialMode { get; set; }

        /// <summary>
        /// Gets or sets the exception type name when available.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the exception message when available.
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the tenant name related to the failure, when applicable.
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// Gets or sets the tenant Key Vault URI related to the failure, when applicable.
        /// </summary>
        public string TenantVaultUri { get; set; }

        /// <summary>
        /// Gets or sets Application Insights search hints.
        /// </summary>
        public string ApplicationInsightsHints { get; set; }

        /// <summary>
        /// Gets or sets the SAS core secret key that stores the service principal connection string.
        /// </summary>
        public string ServicePrincipalSecretKeyName { get; set; }

        /// <summary>
        /// Gets or sets the Azure AD application (client) id involved in the failure.
        /// </summary>
        public string AzureAdClientId { get; set; }

        /// <summary>
        /// Gets or sets remediation steps for operators.
        /// </summary>
        public string RemediationSteps { get; set; }
    }
}
