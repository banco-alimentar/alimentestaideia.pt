// -----------------------------------------------------------------------
// <copyright file="IFiscalReceiptDeliveryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    /// <summary>
    /// Delivers the immutable PDF artifact for one loaded fiscal document.
    /// </summary>
    public interface IFiscalReceiptDeliveryService
    {
        /// <summary>
        /// Gets the existing fiscal receipt artifact or creates its initial immutable artifact.
        /// </summary>
        /// <param name="document">The loaded fiscal document and persisted navigations.</param>
        /// <returns>The immutable fiscal PDF artifact.</returns>
        FiscalReceiptPdfArtifact GetOrCreate(FiscalDocument document);
    }
}
