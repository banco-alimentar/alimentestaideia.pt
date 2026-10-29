// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentLine.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Stores an immutable fiscal document line.
    /// </summary>
    public class FiscalDocumentLine
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the fiscal document identifier.
        /// </summary>
        public int FiscalDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the fiscal document.
        /// </summary>
        public FiscalDocument FiscalDocument { get; set; }

        /// <summary>
        /// Gets or sets the line number within the document.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the product or service code.
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Gets or sets the line description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price before tax.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the tax rate.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Gets or sets the tax exemption reason or code.
        /// </summary>
        public string TaxExemptionReason { get; set; }

        /// <summary>
        /// Gets or sets the net line total.
        /// </summary>
        public decimal NetTotal { get; set; }

        /// <summary>
        /// Gets or sets the tax line total.
        /// </summary>
        public decimal TaxTotal { get; set; }

        /// <summary>
        /// Gets or sets the gross line total.
        /// </summary>
        public decimal GrossTotal { get; set; }
    }
}
