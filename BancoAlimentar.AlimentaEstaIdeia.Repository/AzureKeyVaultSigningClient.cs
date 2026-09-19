// -----------------------------------------------------------------------
// <copyright file="AzureKeyVaultSigningClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Security.Cryptography;
    using Azure.Identity;
    using Azure.Security.KeyVault.Keys.Cryptography;

    /// <summary>
    /// Calls Azure Key Vault cryptography with the application managed identity credential chain.
    /// </summary>
    public class AzureKeyVaultSigningClient : IAzureKeyVaultSigningClient
    {
        /// <inheritdoc />
        public byte[] Sign(AzureKeyVaultSigningRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!Uri.TryCreate(
                    $"{request.KeyIdentifier?.TrimEnd('/')}/{request.KeyVersion}",
                    UriKind.Absolute,
                    out Uri keyIdentifier))
            {
                throw new InvalidOperationException("Fiscal signing requires a valid Azure Key Vault key identifier.");
            }

            var cryptographyClient = new CryptographyClient(keyIdentifier, new DefaultAzureCredential());
            byte[] digest = SHA1.HashData(request.Payload);
            return cryptographyClient.Sign(new SignatureAlgorithm("RS1"), digest).Signature;
        }
    }
}
