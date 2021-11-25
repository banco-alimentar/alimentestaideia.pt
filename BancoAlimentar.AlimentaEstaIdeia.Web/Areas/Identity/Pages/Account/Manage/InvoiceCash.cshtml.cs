// -----------------------------------------------------------------------
// <copyright file="InvoiceCash.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Represent the invoice for the cash donation.
    /// </summary>
    public class InvoiceCashModel : PageModel
    {
        /// <summary>
        /// Gets or sets the name of the FoodBank receipt in the invoice for cashdonation.
        /// </summary>
        public string ReceiptName { get; set; }

        /// <summary>
        /// Gets or sets the path for the image that contains the signature.
        /// </summary>
        public string ReceiptSignatureImg { get; set; }

        /// <summary>
        /// Gets or sets or set the footer place in the invoice.
        /// </summary>
        public string ReceiptPlace { get; set; }

        /// <summary>
        /// Gets or sets the html header part of the invoice.
        /// </summary>
        public string ReceiptHeader { get; set; }
    }
}
