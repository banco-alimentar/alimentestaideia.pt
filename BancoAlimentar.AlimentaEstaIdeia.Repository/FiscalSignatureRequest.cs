// -----------------------------------------------------------------------
// <copyright file="FiscalSignatureRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Identifies an immutable fiscal document snapshot to sign.
    /// </summary>
    public class FiscalSignatureRequest
    {
        /// <summary>
        /// Gets or sets the fiscal document snapshot.
        /// </summary>
        public FiscalDocument FiscalDocument { get; set; }

        /// <summary>
        /// Gets or sets the predecessor hash, or <see langword="null"/> for the first document in a chain.
        /// </summary>
        public string PreviousHash { get; set; }

        /// <summary>
        /// Gets or sets the non-secret signing key identifier.
        /// </summary>
        public string SigningKeyIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the non-secret signing key version.
        /// </summary>
        public string SigningKeyVersion { get; set; }
    }
}
