// -----------------------------------------------------------------------
// <copyright file="FiscalSignatureResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Contains persistable fiscal signature evidence.
    /// </summary>
    public class FiscalSignatureResult
    {
        /// <summary>
        /// Gets or sets the deterministic payload that was signed.
        /// </summary>
        public string CanonicalPayload { get; set; }

        /// <summary>
        /// Gets or sets the predecessor hash, or <see langword="null"/> for the first document in a chain.
        /// </summary>
        public string PreviousHash { get; set; }

        /// <summary>
        /// Gets or sets the fiscal signature hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the hash control value.
        /// </summary>
        public string HashControl { get; set; }

        /// <summary>
        /// Gets or sets the non-secret signing key version used for the signature.
        /// </summary>
        public string SigningKeyVersion { get; set; }
    }
}
