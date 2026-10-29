// -----------------------------------------------------------------------
// <copyright file="AzureKeyVaultSigningRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Describes a versioned Azure Key Vault signing operation.
    /// </summary>
    public class AzureKeyVaultSigningRequest
    {
        /// <summary>Gets or sets the Key Vault key identifier.</summary>
        public string KeyIdentifier { get; set; }

        /// <summary>Gets or sets the key version.</summary>
        public string KeyVersion { get; set; }

        /// <summary>Gets or sets the payload bytes.</summary>
        public byte[] Payload { get; set; }

        /// <summary>Gets or sets the Key Vault signature algorithm.</summary>
        public string Algorithm { get; set; }
    }
}
