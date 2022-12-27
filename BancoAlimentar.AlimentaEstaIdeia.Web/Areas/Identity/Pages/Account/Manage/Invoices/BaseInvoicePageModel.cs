// -----------------------------------------------------------------------
// <copyright file="BaseInvoicePageModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage.Invoices
{
    using global::BancoAlimentar.AlimentaEstaIdeia.Model;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Model;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Base class for the invoice page model.
    /// </summary>
    public abstract class BaseInvoicePageModel : PageModel
    {
        /// <summary>
        /// Gets or sets the invoice render model.
        /// </summary>
        public InvoiceRenderModel InvoiceRenderModel { get; set; }

        /// <summary>
        /// Gets or sets the full name for the user.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the Nif for the user.
        /// </summary>
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the donation amount.
        /// </summary>
        public double DonationAmount { get; set; }

        /// <summary>
        /// Gets or sets the current <see cref="Campaign"/>.
        /// </summary>
        public Campaign Campaign { get; set; }

        /// <summary>
        /// Gets or sets the Invoice name.
        /// </summary>
        public string InvoiceName { get; set; }

        /// <summary>
        /// Gets or sets the donation amount in text.
        /// </summary>
        public string DonationAmountToText { get; set; }

        /// <summary>
        /// Initialize the invoice model.
        /// </summary>
        public abstract void InitializeInvoice();
    }
}
