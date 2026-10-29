// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalReceiptPdfPageModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Razor-only wrapper around a Development-only simulated receipt model.
    /// </summary>
    public class SimulatedFiscalReceiptPdfPageModel : PageModel
    {
        /// <summary>
        /// Gets or sets the non-fiscal simulated receipt to render.
        /// </summary>
        public SimulatedFiscalReceiptViewModel Receipt { get; set; }

        /// <summary>
        /// Gets or sets the private simulation QR PNG.
        /// </summary>
        public byte[] SimulationQrPng { get; set; } = System.Array.Empty<byte>();
    }
}
