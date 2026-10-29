// -----------------------------------------------------------------------
// <copyright file="AzureKeyVaultSigningClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using Azure.Identity;
    using Azure.Security.KeyVault.Keys.Cryptography;

    /// <summary>
    /// Signs payloads using a versioned key in Azure Key Vault.
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

            if (!Uri.TryCreate(request.KeyIdentifier, UriKind.Absolute, out Uri keyIdentifier) ||
                string.IsNullOrWhiteSpace(request.KeyVersion) || request.Payload == null || request.Payload.Length == 0)
            {
                throw new ArgumentException("Key Vault signing requires a key identifier, version, and payload.", nameof(request));
            }

            if (!string.Equals(request.Algorithm, "RS1", StringComparison.Ordinal) &&
                !string.Equals(request.Algorithm, "RS256", StringComparison.Ordinal))
            {
                throw new ArgumentException("The Key Vault signing algorithm is not supported.", nameof(request));
            }

            SignatureAlgorithm algorithm = request.Algorithm;

            string versionedKeyId = $"{keyIdentifier.AbsoluteUri.TrimEnd('/')}/{Uri.EscapeDataString(request.KeyVersion)}";
            var client = new CryptographyClient(new Uri(versionedKeyId), new DefaultAzureCredential());
            return client.SignData(algorithm, request.Payload).Signature;
        }
    }
}
