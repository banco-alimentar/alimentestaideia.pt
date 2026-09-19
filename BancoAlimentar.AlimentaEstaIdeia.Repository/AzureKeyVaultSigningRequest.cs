// -----------------------------------------------------------------------
// <copyright file="AzureKeyVaultSigningRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Identifies one payload signing request sent to Azure Key Vault.
    /// </summary>
    public class AzureKeyVaultSigningRequest
    {
        /// <summary>
        /// Gets or sets the non-secret key identifier.
        /// </summary>
        public string KeyIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the non-secret immutable key version.
        /// </summary>
        public string KeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the UTF-8 canonical fiscal payload bytes.
        /// </summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Gets or sets the Azure Key Vault signature algorithm identifier.
        /// </summary>
        public string Algorithm { get; set; }
    }
}
