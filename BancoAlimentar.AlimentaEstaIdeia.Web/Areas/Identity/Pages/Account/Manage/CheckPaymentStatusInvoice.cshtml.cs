// -----------------------------------------------------------------------
// <copyright file="CheckPaymentStatusInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Page for waiting for the payment to be confirmed.
    /// </summary>
    public class CheckPaymentStatusInvoiceModel : PageModel
    {
        /// <summary>
        /// Refresh of the page.
        /// </summary>
        public const int PageRefreshInSeconds = 5;

        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckPaymentStatusInvoiceModel"/> class.
        /// </summary>
        public CheckPaymentStatusInvoiceModel(IUnitOfWork context)
        {
            this.context = context;
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public IActionResult OnGet(string publicId)
        {
            if (Guid.TryParse(publicId, out Guid value))
            {
                int donationId = this.context.Donation.GetDonationIdFromPublicId(value);
                Donation donation = this.context.Donation.GetById(donationId);
                if (donation.PaymentStatus == PaymentStatus.Payed && donation.ConfirmedPayment != null)
                {
                    return this.RedirectToPage("/Identity/Account/Manage/GenerateInvoice", new { publicDonationId = publicId });
                }

                if (donation.PaymentStatus == PaymentStatus.Payed && donation.ConfirmedPayment == null)
                {
                    Response.Headers.Add("Refresh", PageRefreshInSeconds.ToString());
                }

                return this.Page();
            }
            else
            {
                return this.RedirectToPage("/Index");
            }
        }
    }
}
