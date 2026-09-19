// -----------------------------------------------------------------------
// <copyright file="IAzureKeyVaultSigningClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Defines the remote Azure Key Vault signing boundary.
    /// </summary>
    public interface IAzureKeyVaultSigningClient
    {
        /// <summary>
        /// Signs the supplied payload with a protected Key Vault key.
        /// </summary>
        /// <param name="request">The key reference, algorithm and payload.</param>
        /// <returns>The raw signature bytes.</returns>
        byte[] Sign(AzureKeyVaultSigningRequest request);
    }
}
