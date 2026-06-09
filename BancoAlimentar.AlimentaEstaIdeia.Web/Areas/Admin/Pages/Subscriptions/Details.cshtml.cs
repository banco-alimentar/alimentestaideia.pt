// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Subscription details model.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public DetailsModel(IUnitOfWork context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Gets the donations linked to the subscription.
        /// </summary>
        public IList<Donation> Donations { get; private set; } = new List<Donation>();

        /// <summary>
        /// Gets donation totals grouped by payment status.
        /// </summary>
        public IList<DonationPaymentStatusSummary> DonationsByPaymentStatus { get; private set; } =
            new List<DonationPaymentStatusSummary>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The subscription id.</param>
        /// <returns>A <see cref="IActionResult"/> representing the result of the operation.</returns>
        public IActionResult OnGet(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Subscription = this.context.SubscriptionRepository.GetSubscriptionById(id.Value);
            if (Subscription == null)
            {
                return NotFound();
            }

            Donations = this.context.SubscriptionRepository.GetDonationsForSubscription(id.Value);

            DonationsByPaymentStatus = Donations
                .GroupBy(donation => donation.PaymentStatus)
                .Select(group => new DonationPaymentStatusSummary
                {
                    PaymentStatus = group.Key,
                    DonationCount = group.Count(),
                    TotalAmount = group.Sum(donation => donation.DonationAmount),
                })
                .OrderBy(summary => summary.PaymentStatus)
                .ToList();

            return Page();
        }

        /// <summary>
        /// Donation totals for a payment status.
        /// </summary>
        public sealed class DonationPaymentStatusSummary
        {
            /// <summary>
            /// Gets or sets the payment status.
            /// </summary>
            public PaymentStatus PaymentStatus { get; set; }

            /// <summary>
            /// Gets or sets the number of donations.
            /// </summary>
            public int DonationCount { get; set; }

            /// <summary>
            /// Gets or sets the total donation amount.
            /// </summary>
            public double TotalAmount { get; set; }
        }
    }
}
