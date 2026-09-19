// -----------------------------------------------------------------------
// <copyright file="FiscalSigningKeyRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Contains the canonical payload and key reference supplied to a protected signer.
    /// </summary>
    public class FiscalSigningKeyRequest
    {
        /// <summary>
        /// Gets or sets the deterministic payload to sign.
        /// </summary>
        public string CanonicalPayload { get; set; }

        /// <summary>
        /// Gets or sets the non-secret signing key identifier.
        /// </summary>
        public string SigningKeyIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the non-secret signing key version.
        /// </summary>
        public string SigningKeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the signing algorithm required for the fiscal signature.
        /// </summary>
        public string SigningAlgorithm { get; set; }
    }
}
