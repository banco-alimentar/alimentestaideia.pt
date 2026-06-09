// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// List donations model.
    /// </summary>
    public class IndexModel : PageModel
    {
        private const int PageSize = 25;
        private const int MaxServiceReferenceSearchLength = 20;
        private const int MaxNifSearchLength = 20;
        private const int MaxEmailSearchLength = 256;
        private const int MaxAmountSearchLength = 20;
        private const int MaxPublicIdSearchLength = 36;

        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public IndexModel(IUnitOfWork context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the list of donations for the current page.
        /// </summary>
        public IList<Donation> Donation { get; set; } = new List<Donation>();

        /// <summary>
        /// Gets or sets the service reference search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ServiceReferenceSearch { get; set; }

        /// <summary>
        /// Gets or sets the NIF search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string NifSearch { get; set; }

        /// <summary>
        /// Gets or sets the user email search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string EmailSearch { get; set; }

        /// <summary>
        /// Gets or sets the donation amount search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string AmountSearch { get; set; }

        /// <summary>
        /// Gets or sets the donation date search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public DateTime? DonationDateSearch { get; set; }

        /// <summary>
        /// Gets or sets the public id search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string PublicIdSearch { get; set; }

        /// <summary>
        /// Gets or sets the current page index (1-based).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets the number of donations per page.
        /// </summary>
        public int DonationsPerPage => PageSize;

        /// <summary>
        /// Gets the total number of donations.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a previous page exists.
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// Gets a value indicating whether a next page exists.
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Gets a value indicating whether any search filter is active.
        /// </summary>
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(ServiceReferenceSearch)
            || !string.IsNullOrWhiteSpace(NifSearch)
            || !string.IsNullOrWhiteSpace(EmailSearch)
            || !string.IsNullOrWhiteSpace(AmountSearch)
            || DonationDateSearch.HasValue
            || !string.IsNullOrWhiteSpace(PublicIdSearch);

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            ServiceReferenceSearch = NormalizeSearch(ServiceReferenceSearch, MaxServiceReferenceSearchLength);
            NifSearch = NormalizeSearch(NifSearch, MaxNifSearchLength);
            EmailSearch = NormalizeSearch(EmailSearch, MaxEmailSearchLength);
            AmountSearch = NormalizeSearch(AmountSearch, MaxAmountSearchLength);
            PublicIdSearch = NormalizeSearch(PublicIdSearch, MaxPublicIdSearchLength);

            if (PageIndex < 1)
            {
                PageIndex = 1;
            }

            IQueryable<Donation> query = this.context.Donation.GetAll().AsNoTracking();
            query = ApplyFilters(query);

            TotalCount = await query.CountAsync();
            TotalPages = TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (TotalPages > 0 && PageIndex > TotalPages)
            {
                PageIndex = TotalPages;
            }

            if (TotalCount == 0)
            {
                return;
            }

            Donation = await query
                .Include(donation => donation.ReferralEntity)
                .Include(donation => donation.User)
                .Include(donation => donation.ConfirmedPayment)
                .OrderByDescending(donation => donation.DonationDate)
                .ThenByDescending(donation => donation.Id)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
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

        private IQueryable<Donation> ApplyFilters(IQueryable<Donation> query)
        {
            if (!string.IsNullOrWhiteSpace(ServiceReferenceSearch))
            {
                query = query.Where(donation =>
                    donation.ServiceReference != null
                    && donation.ServiceReference.Contains(ServiceReferenceSearch));
            }

            if (!string.IsNullOrWhiteSpace(NifSearch))
            {
                query = query.Where(donation =>
                    (donation.Nif != null && donation.Nif.Contains(NifSearch))
                    || (donation.User != null && donation.User.Nif != null && donation.User.Nif.Contains(NifSearch)));
            }

            if (!string.IsNullOrWhiteSpace(EmailSearch))
            {
                query = query.Where(donation =>
                    donation.User != null
                    && donation.User.Email != null
                    && donation.User.Email.Contains(EmailSearch));
            }

            if (TryParseAmount(AmountSearch, out decimal amount))
            {
                double amountValue = (double)amount;
                query = query.Where(donation => donation.DonationAmount == amountValue);
            }

            if (DonationDateSearch.HasValue)
            {
                DateTime date = DonationDateSearch.Value.Date;
                query = query.Where(donation => donation.DonationDate.Date == date);
            }

            if (!string.IsNullOrWhiteSpace(PublicIdSearch))
            {
                if (Guid.TryParse(PublicIdSearch, out Guid publicId))
                {
                    query = query.Where(donation => donation.PublicId == publicId);
                }
                else
                {
                    query = query.Where(donation => false);
                }
            }

            return query;
        }

        private string NormalizeSearch(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length > maxLength)
            {
                trimmed = trimmed.Substring(0, maxLength);
            }

            return trimmed;
        }

        private bool TryParseAmount(string value, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
            {
                return true;
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount);
        }
    }
}
