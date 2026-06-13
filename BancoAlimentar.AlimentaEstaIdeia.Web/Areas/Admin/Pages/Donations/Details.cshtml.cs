// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Details on the donation.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Application Db Context.</param>
        public DetailsModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the current donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets the payments related to the donation.
        /// </summary>
        public IList<BasePayment> Payments { get; private set; } = new List<BasePayment>();

        /// <summary>
        /// Gets the invoices related to the donation.
        /// </summary>
        public IList<Invoice> Invoices { get; private set; } = new List<Invoice>();

        /// <summary>
        /// Gets the subscription linked to this donation, if any.
        /// </summary>
        public Subscription RelatedSubscription { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this donation is the initial donation of its subscription.
        /// </summary>
        public bool IsInitialSubscriptionDonation { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this donation belongs to a subscription.
        /// </summary>
        public bool IsSubscriptionDonation => RelatedSubscription != null;

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The donation id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Donation = await context.Donations
                .AsNoTracking()
                .Include(donation => donation.ReferralEntity)
                .Include(donation => donation.Campaign)
                .Include(donation => donation.User)
                .Include(donation => donation.ConfirmedPayment)
                .Include(donation => donation.PaymentList)
                .FirstOrDefaultAsync(donation => donation.Id == id);

            if (Donation == null)
            {
                return NotFound();
            }

            Payments = Donation.PaymentList?
                .OrderByDescending(payment => payment.Created)
                .ThenByDescending(payment => payment.Id)
                .ToList() ?? new List<BasePayment>();

            Invoices = await context.Invoices
                .AsNoTracking()
                .Where(invoice => EF.Property<int?>(invoice, "DonationId") == id)
                .OrderByDescending(invoice => invoice.Created)
                .ToListAsync();

            await this.LoadSubscriptionInfoAsync(id.Value);

            return Page();
        }

        /// <summary>
        /// Gets the payment type label for display.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>The payment type name.</returns>
        public string GetPaymentTypeName(BasePayment payment)
        {
            return payment switch
            {
                MultiBankPayment => "MultiBank",
                CreditCardPayment => "CreditCard",
                MBWayPayment => "MBWay",
                PayPalPayment => "PayPal",
                _ => payment.GetType().Name,
            };
        }

        /// <summary>
        /// Gets the Easypay payment identifier when available.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>The Easypay payment id, if any.</returns>
        public string GetEasyPayPaymentId(BasePayment payment)
        {
            if (payment is EasyPayBaseClass easyPayPayment)
            {
                return easyPayPayment.EasyPayPaymentId;
            }

            return null;
        }

        /// <summary>
        /// Gets a value indicating whether the payment is the confirmed payment for the donation.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>True when this is the confirmed payment.</returns>
        public bool IsConfirmedPayment(BasePayment payment)
        {
            return Donation?.ConfirmedPayment != null && Donation.ConfirmedPayment.Id == payment.Id;
        }

        private async Task LoadSubscriptionInfoAsync(int donationId)
        {
            int? subscriptionId = await context.SubscriptionDonations
                .AsNoTracking()
                .Where(link => link.Donation != null && link.Donation.Id == donationId)
                .Select(link => (int?)link.Subscription.Id)
                .FirstOrDefaultAsync();

            if (!subscriptionId.HasValue)
            {
                return;
            }

            RelatedSubscription = await context.Subscriptions
                .AsNoTracking()
                .Include(subscription => subscription.InitialDonation)
                .FirstOrDefaultAsync(subscription => subscription.Id == subscriptionId.Value);

            if (RelatedSubscription?.InitialDonation != null)
            {
                IsInitialSubscriptionDonation = RelatedSubscription.InitialDonation.Id == donationId;
            }
        }
    }
}
