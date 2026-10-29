// -----------------------------------------------------------------------
// <copyright file="IFiscalSigningKeyProvider.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Defines the protected signing-key boundary.
    /// </summary>
    public interface IFiscalSigningKeyProvider
    {
        /// <summary>
        /// Signs one canonical fiscal payload with a non-secret key reference.
        /// </summary>
        /// <param name="request">The canonical payload and signing key reference.</param>
        /// <returns>The fiscal hash and control evidence.</returns>
        FiscalSigningKeyResult Sign(FiscalSigningKeyRequest request);
    }
}
