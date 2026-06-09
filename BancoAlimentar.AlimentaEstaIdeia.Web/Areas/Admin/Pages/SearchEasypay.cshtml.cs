// -----------------------------------------------------------------------
// <copyright file="SearchEasypay.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Search donations and payments by Easypay transaction key or subscription id.
    /// </summary>
    public class SearchEasypayModel : PageModel
    {
        private const int MaxSearchValueLength = 255;

        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchEasypayModel"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public SearchEasypayModel(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets or sets the Easypay transaction key to search for.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        [Display(Name = "TransactionKey")]
        [StringLength(MaxSearchValueLength)]
        public string TransactionKey { get; set; }

        /// <summary>
        /// Gets or sets the Easypay subscription id to search for.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        [Display(Name = "EasyPaySubscriptionId")]
        [StringLength(MaxSearchValueLength)]
        public string EasyPaySubscriptionId { get; set; }

        /// <summary>
        /// Gets the payments matching the search criteria.
        /// </summary>
        public IList<BasePayment> Payments { get; private set; } = new List<BasePayment>();

        /// <summary>
        /// Gets the donations linked to the matching payments and subscriptions.
        /// </summary>
        public IList<Donation> Donations { get; private set; } = new List<Donation>();

        /// <summary>
        /// Gets the subscriptions matching the search criteria.
        /// </summary>
        public IList<Subscription> Subscriptions { get; private set; } = new List<Subscription>();

        /// <summary>
        /// Gets a value indicating whether a search was performed.
        /// </summary>
        public bool HasSearched { get; private set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            string transactionKey = NormalizeSearchValue(TransactionKey);
            string easyPaySubscriptionId = NormalizeSearchValue(EasyPaySubscriptionId);
            TransactionKey = transactionKey;
            EasyPaySubscriptionId = easyPaySubscriptionId;

            if (string.IsNullOrWhiteSpace(transactionKey) && string.IsNullOrWhiteSpace(easyPaySubscriptionId))
            {
                return;
            }

            HasSearched = true;

            var payments = new List<BasePayment>();
            var subscriptions = new List<Subscription>();

            if (!string.IsNullOrWhiteSpace(transactionKey))
            {
                await SearchByTransactionKeyAsync(transactionKey, payments, subscriptions);
            }

            if (!string.IsNullOrWhiteSpace(easyPaySubscriptionId))
            {
                await SearchByEasyPaySubscriptionIdAsync(easyPaySubscriptionId, payments, subscriptions);
            }

            Payments = payments
                .GroupBy(payment => payment.Id)
                .Select(group => group.First())
                .OrderByDescending(payment => payment.Created)
                .ThenByDescending(payment => payment.Id)
                .ToList();

            Subscriptions = subscriptions
                .GroupBy(subscription => subscription.Id)
                .Select(group => group.First())
                .OrderByDescending(subscription => subscription.Created)
                .ToList();

            Donations = BuildDonationsList(Payments, Subscriptions);
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

        private static IList<Donation> BuildDonationsList(IList<BasePayment> payments, IList<Subscription> subscriptions)
        {
            var donations = new Dictionary<int, Donation>();

            foreach (BasePayment payment in payments)
            {
                if (payment.Donation != null)
                {
                    donations[payment.Donation.Id] = payment.Donation;
                }
            }

            foreach (Subscription subscription in subscriptions)
            {
                if (subscription.InitialDonation != null)
                {
                    donations[subscription.InitialDonation.Id] = subscription.InitialDonation;
                }

                if (subscription.Donations == null)
                {
                    continue;
                }

                foreach (SubscriptionDonations subscriptionDonation in subscription.Donations)
                {
                    if (subscriptionDonation.Donation != null)
                    {
                        donations[subscriptionDonation.Donation.Id] = subscriptionDonation.Donation;
                    }
                }
            }

            return donations.Values
                .OrderByDescending(donation => donation.DonationDate)
                .ToList();
        }

        private static IEnumerable<int> GetSubscriptionDonationIds(Subscription subscription)
        {
            if (subscription.InitialDonation != null)
            {
                yield return subscription.InitialDonation.Id;
            }

            if (subscription.Donations == null)
            {
                yield break;
            }

            foreach (SubscriptionDonations subscriptionDonation in subscription.Donations)
            {
                if (subscriptionDonation.Donation != null)
                {
                    yield return subscriptionDonation.Donation.Id;
                }
            }
        }

        private static string NormalizeSearchValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length > MaxSearchValueLength)
            {
                trimmed = trimmed.Substring(0, MaxSearchValueLength);
            }

            return trimmed;
        }

        private async Task SearchByTransactionKeyAsync(string transactionKey, List<BasePayment> payments, List<Subscription> subscriptions)
        {
            List<BasePayment> transactionKeyPayments = await dbContext.Payments
                .AsNoTracking()
                .Include(payment => payment.Donation)
                    .ThenInclude(donation => donation.User)
                .Where(payment =>
                    payment.TransactionKey != null
                    && EF.Functions.Like(payment.TransactionKey, transactionKey))
                .OrderByDescending(payment => payment.Created)
                .ThenByDescending(payment => payment.Id)
                .ToListAsync();

            payments.AddRange(transactionKeyPayments);

            List<Subscription> transactionKeySubscriptions = await dbContext.Subscriptions
                .AsNoTracking()
                .Include(subscription => subscription.InitialDonation)
                .Where(subscription =>
                    subscription.TransactionKey != null
                    && EF.Functions.Like(subscription.TransactionKey, transactionKey))
                .OrderByDescending(subscription => subscription.Created)
                .ToListAsync();

            subscriptions.AddRange(transactionKeySubscriptions);
        }

        private async Task SearchByEasyPaySubscriptionIdAsync(string easyPaySubscriptionId, List<BasePayment> payments, List<Subscription> subscriptions)
        {
            List<Subscription> matchingSubscriptions = await dbContext.Subscriptions
                .AsNoTracking()
                .Include(subscription => subscription.InitialDonation)
                    .ThenInclude(donation => donation.User)
                .Include(subscription => subscription.Donations)
                    .ThenInclude(subscriptionDonation => subscriptionDonation.Donation)
                        .ThenInclude(donation => donation.User)
                .Where(subscription =>
                    subscription.EasyPaySubscriptionId != null
                    && EF.Functions.Like(subscription.EasyPaySubscriptionId, easyPaySubscriptionId))
                .OrderByDescending(subscription => subscription.Created)
                .ToListAsync();

            subscriptions.AddRange(matchingSubscriptions);

            List<int> donationIds = matchingSubscriptions
                .SelectMany(GetSubscriptionDonationIds)
                .Distinct()
                .ToList();

            if (donationIds.Count == 0)
            {
                return;
            }

            List<BasePayment> subscriptionPayments = await dbContext.Payments
                .AsNoTracking()
                .Include(payment => payment.Donation)
                    .ThenInclude(donation => donation.User)
                .Where(payment => payment.Donation != null && donationIds.Contains(payment.Donation.Id))
                .OrderByDescending(payment => payment.Created)
                .ThenByDescending(payment => payment.Id)
                .ToListAsync();

            payments.AddRange(subscriptionPayments);
        }
    }
}
