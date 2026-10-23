// -----------------------------------------------------------------------
// <copyright file="FiscalSigningKeyResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Contains signature evidence returned by the protected signer.
    /// </summary>
    public class FiscalSigningKeyResult
    {
        /// <summary>
        /// Gets or sets the fiscal signature hash.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets or sets the hash control value.
        /// </summary>
        public string HashControl { get; set; }
    }
}
