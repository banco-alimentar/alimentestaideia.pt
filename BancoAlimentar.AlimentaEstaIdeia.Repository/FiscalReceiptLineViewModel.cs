// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptLineViewModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    /// <summary>
    /// Represents one immutable fiscal document line for receipt rendering.
    /// </summary>
    public sealed class FiscalReceiptLineViewModel
    {
        /// <summary>Gets the persisted line number.</summary>
        public int LineNumber { get; init; }

        /// <summary>Gets the persisted product or service code.</summary>
        public string ProductCode { get; init; } = string.Empty;

        /// <summary>Gets the persisted line description.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>Gets the persisted quantity.</summary>
        public decimal Quantity { get; init; }

        /// <summary>Gets the persisted unit price before tax.</summary>
        public decimal UnitPrice { get; init; }

        /// <summary>Gets the persisted tax rate.</summary>
        public decimal TaxRate { get; init; }

        /// <summary>Gets the persisted tax exemption reason.</summary>
        public string TaxExemptionReason { get; init; } = string.Empty;

        /// <summary>Gets the persisted net line total.</summary>
        public decimal NetTotal { get; init; }

        /// <summary>Gets the persisted tax line total.</summary>
        public decimal TaxTotal { get; init; }

        /// <summary>Gets the persisted gross line total.</summary>
        public decimal GrossTotal { get; init; }
    }
}
