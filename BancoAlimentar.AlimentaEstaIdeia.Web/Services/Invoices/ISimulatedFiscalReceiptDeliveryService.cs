// -----------------------------------------------------------------------
// <copyright file="ISimulatedFiscalReceiptDeliveryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Delivers the immutable PDF artifact for one Development-only simulated receipt.
    /// </summary>
    public interface ISimulatedFiscalReceiptDeliveryService
    {
        /// <summary>
        /// Gets the existing simulated receipt PDF or creates its initial immutable artifact.
        /// </summary>
        /// <param name="document">The persisted non-fiscal simulation document.</param>
        /// <returns>The immutable simulated PDF bytes.</returns>
        byte[] GetOrCreate(SimulatedFiscalDocument document);
    }
}
