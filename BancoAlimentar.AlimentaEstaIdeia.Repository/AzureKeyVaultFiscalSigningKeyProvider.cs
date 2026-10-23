// -----------------------------------------------------------------------
// <copyright file="AzureKeyVaultFiscalSigningKeyProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Text;

    /// <summary>
    /// Signs canonical fiscal payloads through Azure Key Vault.
    /// </summary>
    public class AzureKeyVaultFiscalSigningKeyProvider : IFiscalSigningKeyProvider
    {
        private const string KeyVaultRs1Algorithm = "RS1";

        private readonly IAzureKeyVaultSigningClient azureKeyVaultSigningClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKeyVaultFiscalSigningKeyProvider"/> class.
        /// </summary>
        /// <param name="azureKeyVaultSigningClient">The remote protected-key signing client.</param>
        public AzureKeyVaultFiscalSigningKeyProvider(IAzureKeyVaultSigningClient azureKeyVaultSigningClient)
        {
            this.azureKeyVaultSigningClient = azureKeyVaultSigningClient ?? throw new ArgumentNullException(nameof(azureKeyVaultSigningClient));
        }

        /// <inheritdoc />
        public FiscalSigningKeyResult Sign(FiscalSigningKeyRequest request)
        {
            this.ValidateRequest(request);

            byte[] signature;
            try
            {
                signature = this.azureKeyVaultSigningClient.Sign(new AzureKeyVaultSigningRequest
                {
                    KeyIdentifier = request.SigningKeyIdentifier,
                    KeyVersion = request.SigningKeyVersion,
                    Payload = Encoding.UTF8.GetBytes(request.CanonicalPayload),
                    Algorithm = KeyVaultRs1Algorithm,
                });
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Fiscal signing operation failed.", exception);
            }

            if (signature == null || signature.Length == 0)
            {
                throw new InvalidOperationException("Fiscal signing operation returned no signature.");
            }

            return new FiscalSigningKeyResult
            {
                Hash = Convert.ToBase64String(signature),
                HashControl = request.SigningKeyVersion,
            };
        }

        private void ValidateRequest(FiscalSigningKeyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.SigningKeyIdentifier) ||
                string.IsNullOrWhiteSpace(request.SigningKeyVersion))
            {
                throw new InvalidOperationException("Fiscal signing requires a key identifier and version.");
            }

            if (string.IsNullOrWhiteSpace(request.CanonicalPayload))
            {
                throw new InvalidOperationException("Fiscal signing requires a canonical payload.");
            }

            if (!string.Equals(request.SigningAlgorithm, "RSA", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fiscal signing requires the RSA signing algorithm.");
            }
        }
    }
}
