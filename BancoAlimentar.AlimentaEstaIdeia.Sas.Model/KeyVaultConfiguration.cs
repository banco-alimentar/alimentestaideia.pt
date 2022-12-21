// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System;

    /// <summary>
    /// Define the Key Vault configuration.
    /// </summary>
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets Azure Key Vault url.
        /// </summary>
        public Uri Vault { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets when the Configuration was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the KeyVault has his own Service Principal instead of the MSI.
        /// </summary>
        public bool HasServicePrincipalEnabled { get; set; }

        /// <summary>
        /// Gets or sets the key used to store the Service Principal in the Key Vault Service.
        /// </summary>
        public string SasSPKeyVaultKeyName { get; set; }
    }
}
