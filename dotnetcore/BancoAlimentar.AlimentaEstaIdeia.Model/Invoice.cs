// <copyright file="Invoice.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Represent an invoice.
    /// </summary>
    public class Invoice
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets when the invoice is created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the user related to the invoice.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the donation related to the invoice.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the name of the invoice in the Azure Storage Account.
        /// </summary>
        public Guid InvoicePublicId { get; set; }
    }
}
