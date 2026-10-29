// -----------------------------------------------------------------------
// <copyright file="IFiscalSignatureService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Defines the fiscal document signing boundary.
    /// </summary>
    public interface IFiscalSignatureService
    {
        /// <summary>
        /// Signs the canonical fiscal snapshot and returns persistable signature evidence.
        /// </summary>
        /// <param name="request">The document snapshot, predecessor hash and key reference.</param>
        /// <returns>The canonical payload and signature evidence.</returns>
        FiscalSignatureResult Sign(FiscalSignatureRequest request);
    }
}
