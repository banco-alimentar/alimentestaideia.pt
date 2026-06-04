// -----------------------------------------------------------------------
// <copyright file="InvoiceDownloadOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    /// <summary>
    /// Configuration for signed invoice download links.
    /// </summary>
    public class InvoiceDownloadOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "InvoiceDownload";

        /// <summary>
        /// Gets or sets how long emailed invoice links remain valid.
        /// </summary>
        public int TokenLifetimeHours { get; set; } = 72;
    }
}
