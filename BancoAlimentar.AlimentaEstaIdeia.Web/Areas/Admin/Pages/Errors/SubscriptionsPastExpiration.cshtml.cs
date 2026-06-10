// -----------------------------------------------------------------------
// <copyright file="SubscriptionsPastExpiration.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Lists subscriptions with donations dated after the subscription expiration.
    /// </summary>
    public class SubscriptionsPastExpirationModel : PageModel
    {
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsPastExpirationModel"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public SubscriptionsPastExpirationModel(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets subscriptions with late donations.
        /// </summary>
        public IList<SubscriptionPastExpirationIssue> Issues { get; private set; } =
            new List<SubscriptionPastExpirationIssue>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            List<SubscriptionDonations> lateLinks = await this.dbContext.SubscriptionDonations
                .AsNoTracking()
                .Include(link => link.Subscription)
                .ThenInclude(subscription => subscription.User)
                .Include(link => link.Donation)
                .Where(link =>
                    link.Subscription != null
                    && !link.Subscription.IsDeleted
                    && link.Donation != null
                    && link.Donation.DonationDate > link.Subscription.ExpirationTime)
                .ToListAsync();

            Issues = lateLinks
                .GroupBy(link => link.Subscription.Id)
                .Select(group =>
                {
                    Subscription subscription = group.First().Subscription;
                    List<Donation> lateDonations = group.Select(link => link.Donation).ToList();

                    return new SubscriptionPastExpirationIssue
                    {
                        SubscriptionId = subscription.Id,
                        PublicId = subscription.PublicId,
                        Status = subscription.Status,
                        Frequency = subscription.Frequency,
                        ExpirationTime = subscription.ExpirationTime,
                        OwnerEmail = subscription.User?.Email,
                        LateDonationCount = lateDonations.Count,
                        LatePaidAmount = lateDonations
                            .Where(donation => donation.PaymentStatus == PaymentStatus.Payed)
                            .Sum(donation => donation.DonationAmount),
                        LatestLateDonationDate = lateDonations.Max(donation => donation.DonationDate),
                    };
                })
                .OrderByDescending(issue => issue.LatestLateDonationDate)
                .ThenByDescending(issue => issue.LateDonationCount)
                .ToList();
        }

        /// <summary>
        /// Subscription with donations after expiration.
        /// </summary>
        public sealed class SubscriptionPastExpirationIssue
        {
            /// <summary>
            /// Gets or sets the subscription id.
            /// </summary>
            public int SubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the subscription public id.
            /// </summary>
            public Guid PublicId { get; set; }

            /// <summary>
            /// Gets or sets the subscription status.
            /// </summary>
            public SubscriptionStatus Status { get; set; }

            /// <summary>
            /// Gets or sets the subscription frequency.
            /// </summary>
            public string Frequency { get; set; }

            /// <summary>
            /// Gets or sets the subscription expiration time.
            /// </summary>
            public DateTime ExpirationTime { get; set; }

            /// <summary>
            /// Gets or sets the owner email.
            /// </summary>
            public string OwnerEmail { get; set; }

            /// <summary>
            /// Gets or sets the number of late donations.
            /// </summary>
            public int LateDonationCount { get; set; }

            /// <summary>
            /// Gets or sets the total paid amount from late donations.
            /// </summary>
            public double LatePaidAmount { get; set; }

            /// <summary>
            /// Gets or sets the latest late donation date.
            /// </summary>
            public DateTime LatestLateDonationDate { get; set; }
        }
    }
}
