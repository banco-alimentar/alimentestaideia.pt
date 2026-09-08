// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Users
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Admin user details page.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private const int PageSize = 20;
        private const int PaymentNotificationPageSize = 20;
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
        /// Gets or sets the donor user.
        /// </summary>
        public WebUser DonorUser { get; set; }

        /// <summary>
        /// Gets or sets the roles assigned to the user.
        /// </summary>
        public IList<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the donations made by the user.
        /// </summary>
        public IList<Donation> Donations { get; set; } = new List<Donation>();

        /// <summary>
        /// Gets or sets the current donation page index (1-based).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets the number of donations shown on each page.
        /// </summary>
        public int DonationsPerPage => PageSize;

        /// <summary>
        /// Gets or sets the donation column used for sorting.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "DonationDate";

        /// <summary>
        /// Gets or sets a value indicating whether donations are sorted descending.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        /// <summary>
        /// Gets the total number of donations for the user.
        /// </summary>
        public int TotalDonationCount { get; private set; }

        /// <summary>
        /// Gets the total number of donation pages.
        /// </summary>
        public int TotalDonationPages { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a previous donation page exists.
        /// </summary>
        public bool HasPreviousDonationPage => PageIndex > 1;

        /// <summary>
        /// Gets a value indicating whether a next donation page exists.
        /// </summary>
        public bool HasNextDonationPage => PageIndex < TotalDonationPages;

        /// <summary>
        /// Gets or sets the current payment notification page index (1-based).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PaymentNotificationPageIndex { get; set; } = 1;

        /// <summary>
        /// Gets the number of payment notifications shown on each page.
        /// </summary>
        public int PaymentNotificationsPerPage => PaymentNotificationPageSize;

        /// <summary>
        /// Gets the total number of payment notifications sent to the user.
        /// </summary>
        public int TotalPaymentNotificationCount { get; private set; }

        /// <summary>
        /// Gets the total number of payment notification pages.
        /// </summary>
        public int TotalPaymentNotificationPages { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a previous payment notification page exists.
        /// </summary>
        public bool HasPreviousPaymentNotificationPage => PaymentNotificationPageIndex > 1;

        /// <summary>
        /// Gets a value indicating whether a next payment notification page exists.
        /// </summary>
        public bool HasNextPaymentNotificationPage => PaymentNotificationPageIndex < TotalPaymentNotificationPages;

        /// <summary>
        /// Gets or sets the payment notification emails on the current page.
        /// </summary>
        public IList<PaymentNotifications> PaymentNotifications { get; set; } = new List<PaymentNotifications>();

        /// <summary>
        /// Gets or sets all email communications sent to the user.
        /// </summary>
        public IList<EmailCommunication> EmailCommunications { get; set; } = new List<EmailCommunication>();

        /// <summary>
        /// Gets the subscription information keyed by donation id.
        /// </summary>
        public IReadOnlyDictionary<int, SubscriptionDonationInfo> SubscriptionInfoByDonationId { get; private set; } =
            new Dictionary<int, SubscriptionDonationInfo>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            DonorUser = await context.Users
                .AsNoTracking()
                .Include(user => user.Address)
                .FirstOrDefaultAsync(user => user.Id == id);

            if (DonorUser == null)
            {
                return NotFound();
            }

            SortBy = NormalizeSortBy(SortBy);
            if (PageIndex < 1)
            {
                PageIndex = 1;
            }

            Roles = await (
                from userRole in context.UserRoles.AsNoTracking()
                join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == id
                orderby role.Name
                select role.Name)
                .ToListAsync();

            IQueryable<Donation> donationQuery = context.Donations
                .AsNoTracking()
                .Where(donation => EF.Property<string>(donation, "UserId") == id);

            TotalDonationCount = await donationQuery.CountAsync();
            TotalDonationPages = TotalDonationCount == 0
                ? 0
                : (int)Math.Ceiling(TotalDonationCount / (double)PageSize);

            if (TotalDonationPages > 0 && PageIndex > TotalDonationPages)
            {
                PageIndex = TotalDonationPages;
            }

            donationQuery = ApplyDonationSort(donationQuery);
            Donations = await donationQuery
                .Include(donation => donation.ReferralEntity)
                .Include(donation => donation.ConfirmedPayment)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            await this.LoadSubscriptionInfoAsync();

            if (PaymentNotificationPageIndex < 1)
            {
                PaymentNotificationPageIndex = 1;
            }

            IQueryable<PaymentNotifications> paymentNotificationQuery = context.PaymentNotifications
                .AsNoTracking()
                .Where(notification => EF.Property<string>(notification, "UserId") == id);

            TotalPaymentNotificationCount = await paymentNotificationQuery.CountAsync();
            TotalPaymentNotificationPages = TotalPaymentNotificationCount == 0
                ? 0
                : (int)Math.Ceiling(TotalPaymentNotificationCount / (double)PaymentNotificationPageSize);

            if (TotalPaymentNotificationPages > 0 && PaymentNotificationPageIndex > TotalPaymentNotificationPages)
            {
                PaymentNotificationPageIndex = TotalPaymentNotificationPages;
            }

            PaymentNotifications = await paymentNotificationQuery
                .Include(notification => notification.Payment)
                .ThenInclude(payment => payment.Donation)
                .OrderByDescending(notification => notification.Created)
                .ThenByDescending(notification => notification.Id)
                .Skip((PaymentNotificationPageIndex - 1) * PaymentNotificationPageSize)
                .Take(PaymentNotificationPageSize)
                .ToListAsync();

            EmailCommunications = await context.EmailCommunications
                .AsNoTracking()
                .Include(communication => communication.Payment)
                .ThenInclude(payment => payment.Donation)
                .Where(communication => communication.UserId == id)
                .OrderByDescending(communication => communication.SentAtUtc)
                .ThenByDescending(communication => communication.Id)
                .ToListAsync();

            return Page();
        }

        /// <summary>
        /// Gets the next sort direction when a donation column is clicked.
        /// </summary>
        /// <param name="column">The donation column.</param>
        /// <returns>True when the next sort should be descending.</returns>
        public bool GetNextSortDescending(string column)
        {
            string normalizedColumn = NormalizeSortBy(column);
            if (string.Equals(SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return !SortDescending;
            }

            return true;
        }

        /// <summary>
        /// Gets the sort direction indicator for a donation column.
        /// </summary>
        /// <param name="column">The donation column.</param>
        /// <returns>The sort indicator text.</returns>
        public string GetSortIndicator(string column)
        {
            if (!string.Equals(SortBy, NormalizeSortBy(column), StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return SortDescending ? " ▼" : " ▲";
        }

        /// <summary>
        /// Gets the payment type label for display.
        /// </summary>
        /// <param name="donation">The donation.</param>
        /// <returns>The payment type name.</returns>
        public string GetPaymentTypeName(Donation donation)
        {
            if (donation?.ConfirmedPayment == null)
            {
                return null;
            }

            return GetPaymentTypeName(donation.ConfirmedPayment);
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

        private string NormalizeSortBy(string value)
        {
            return value switch
            {
                "DonationAmount" => "DonationAmount",
                _ => "DonationDate",
            };
        }

        private async Task LoadSubscriptionInfoAsync()
        {
            var donationIds = Donations.Select(donation => donation.Id).ToList();
            if (donationIds.Count == 0)
            {
                return;
            }

            var links = await context.SubscriptionDonations
                .AsNoTracking()
                .Include(link => link.Subscription)
                .ThenInclude(subscription => subscription.InitialDonation)
                .Where(link => link.DonationId.HasValue && donationIds.Contains(link.DonationId.Value))
                .OrderBy(link => link.Id)
                .ToListAsync();

            SubscriptionInfoByDonationId = links
                .Where(link => link.DonationId.HasValue && link.Subscription != null)
                .GroupBy(link => link.DonationId.Value)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var link = group.First();
                        return new SubscriptionDonationInfo
                        {
                            Subscription = link.Subscription,
                            IsInitial = link.Subscription.InitialDonation?.Id == link.DonationId.Value,
                        };
                });
        }

        private IQueryable<Donation> ApplyDonationSort(IQueryable<Donation> query)
        {
            return SortBy switch
            {
                "DonationAmount" => SortDescending
                    ? query.OrderByDescending(donation => donation.DonationAmount).ThenByDescending(donation => donation.Id)
                    : query.OrderBy(donation => donation.DonationAmount).ThenBy(donation => donation.Id),
                _ => SortDescending
                    ? query.OrderByDescending(donation => donation.DonationDate).ThenByDescending(donation => donation.Id)
                    : query.OrderBy(donation => donation.DonationDate).ThenBy(donation => donation.Id),
            };
        }

        /// <summary>
        /// Describes the subscription relationship for a donation.
        /// </summary>
        public sealed class SubscriptionDonationInfo
        {
            /// <summary>
            /// Gets or sets the related subscription.
            /// </summary>
            public Subscription Subscription { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the donation created the subscription.
            /// </summary>
            public bool IsInitial { get; set; }
        }
    }
}
