// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptPdfPageModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Razor-only wrapper around a final immutable fiscal receipt model.
    /// </summary>
    public class FiscalReceiptPdfPageModel : PageModel
    {
        /// <summary>
        /// Gets or sets the immutable fiscal receipt to render.
        /// </summary>
        public FiscalReceiptViewModel Receipt { get; set; }
    }
}
