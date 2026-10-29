// -----------------------------------------------------------------------
// <copyright file="IAzureKeyVaultSigningClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Defines the boundary for signing fiscal payloads with Azure Key Vault.
    /// </summary>
    public interface IAzureKeyVaultSigningClient
    {
        /// <summary>
        /// Signs the supplied payload with the requested Key Vault key.
        /// </summary>
        /// <param name="request">The key and payload information.</param>
        /// <returns>The signature bytes.</returns>
        byte[] Sign(AzureKeyVaultSigningRequest request);
    }
}
