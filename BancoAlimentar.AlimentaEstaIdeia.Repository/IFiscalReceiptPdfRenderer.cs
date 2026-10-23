// -----------------------------------------------------------------------
// <copyright file="IFiscalReceiptPdfRenderer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Renders an immutable fiscal receipt model into a PDF byte stream.
    /// </summary>
    public interface IFiscalReceiptPdfRenderer
    {
        /// <summary>
        /// Renders the supplied immutable receipt model.
        /// </summary>
        /// <param name="viewModel">The final receipt model.</param>
        /// <returns>The rendered PDF bytes.</returns>
        byte[] Render(FiscalReceiptViewModel viewModel);
    }
}
