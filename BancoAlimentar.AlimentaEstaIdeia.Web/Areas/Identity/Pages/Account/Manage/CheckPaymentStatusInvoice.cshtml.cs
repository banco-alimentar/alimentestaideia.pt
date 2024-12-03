// -----------------------------------------------------------------------
// <copyright file="CheckPaymentStatusInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Graph.Models;

    /// <summary>
    /// Page for waiting for the payment to be confirmed.
    /// </summary>
    [AllowAnonymous]
    public class CheckPaymentStatusInvoiceModel : PageModel
    {
        /// <summary>
        /// Refresh of the page.
        /// </summary>
        public const int PageRefreshInSeconds = 5;

        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckPaymentStatusInvoiceModel"/> class.
        /// </summary>
        public CheckPaymentStatusInvoiceModel(IUnitOfWork context, TelemetryClient telemetryClient)
        {
            this.context = context;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public IActionResult OnGet(string publicId)
        {
            if (Guid.TryParse(publicId, out Guid value))
            {
                Donation donation = this.context.Donation
                    .Find(p => p.PublicId == value)
                    .Include(p => p.ConfirmedPayment).FirstOrDefault();
                if (donation != null &&
                    donation.PaymentStatus == PaymentStatus.Payed &&
                    donation.ConfirmedPayment != null)
                {
                    return this.RedirectToPage("GenerateInvoice", new { publicDonationId = publicId });
                }

                if (donation == null)
                {
                    telemetryClient.TrackEvent(
                        "CheckStatus-DonationNotFound",
                        new Dictionary<string, string>() { { "DonationId", publicId } });
                }

                Response.Headers.Append("Refresh", PageRefreshInSeconds.ToString());
                return this.Page();
            }
            else
            {
                return this.RedirectToPage("/Index");
            }
        }
    }
}
