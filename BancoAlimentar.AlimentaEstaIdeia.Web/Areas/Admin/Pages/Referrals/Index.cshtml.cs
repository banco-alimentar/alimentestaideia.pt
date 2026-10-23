// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Referrals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Lists active referrals for administrators.
    /// </summary>
    public class IndexModel : PageModel
    {
        private const int PageSize = 25;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="context">Application database context.</param>
        public IndexModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets active referrals.
        /// </summary>
        public IList<ReferralListItem> Referrals { get; private set; } = new List<ReferralListItem>();

        /// <summary>Gets or sets the current page index.</summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>Gets or sets the referral column used for sorting.</summary>
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "CreateDate";

        /// <summary>Gets or sets a value indicating whether referrals are sorted descending.</summary>
        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        /// <summary>Gets the total number of active referrals.</summary>
        public int TotalCount { get; private set; }

        /// <summary>Gets the total number of pages.</summary>
        public int TotalPages { get; private set; }

        /// <summary>Gets the number of referrals shown on each page.</summary>
        public int ReferralsPerPage => PageSize;

        /// <summary>Gets a value indicating whether a previous page exists.</summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>Gets a value indicating whether a next page exists.</summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Loads active referrals and their donation counts.
        /// </summary>
        /// <returns>A task representing the operation.</returns>
        public async Task OnGetAsync()
        {
            if (this.PageIndex < 1)
            {
                this.PageIndex = 1;
            }

            this.SortBy = this.NormalizeSortBy(this.SortBy);
            IQueryable<Referral> query = this.context.Referrals
                .AsNoTracking()
                .Where(referral => referral.Active);

            this.TotalCount = await query.CountAsync();
            this.TotalPages = this.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(this.TotalCount / (double)PageSize);

            if (this.TotalPages > 0 && this.PageIndex > this.TotalPages)
            {
                this.PageIndex = this.TotalPages;
            }

            this.Referrals = await this.ApplySort(query)
                .Skip((this.PageIndex - 1) * PageSize)
                .Take(PageSize)
                .Select(referral => new ReferralListItem
                {
                    Id = referral.Id,
                    Code = referral.Code,
                    Name = referral.Name,
                    CreateDate = referral.CreateDate,
                    LinkOpenCount = referral.LinkOpenCount,
                    DonationCount = referral.Donations.Count(),
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets the next sort direction when a referral column is clicked.
        /// </summary>
        /// <param name="column">The referral column.</param>
        /// <returns>True when the next sort should be descending.</returns>
        public bool GetNextSortDescending(string column)
        {
            string normalizedColumn = this.NormalizeSortBy(column);
            if (string.Equals(this.SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return !this.SortDescending;
            }

            return true;
        }

        /// <summary>
        /// Gets the sort indicator for a referral column.
        /// </summary>
        /// <param name="column">The referral column.</param>
        /// <returns>The sort indicator text.</returns>
        public string GetSortIndicator(string column)
        {
            if (!string.Equals(this.SortBy, this.NormalizeSortBy(column), StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return this.SortDescending ? " ▼" : " ▲";
        }

        private string NormalizeSortBy(string value)
        {
            return value switch
            {
                "Code" => "Code",
                "Name" => "Name",
                "LinkOpenCount" => "LinkOpenCount",
                "DonationCount" => "DonationCount",
                _ => "CreateDate",
            };
        }

        private IOrderedQueryable<Referral> ApplySort(IQueryable<Referral> query)
        {
            return (this.SortBy, this.SortDescending) switch
            {
                ("Code", true) => query.OrderByDescending(referral => referral.Code).ThenByDescending(referral => referral.Id),
                ("Code", false) => query.OrderBy(referral => referral.Code).ThenBy(referral => referral.Id),
                ("Name", true) => query.OrderByDescending(referral => referral.Name).ThenByDescending(referral => referral.Id),
                ("Name", false) => query.OrderBy(referral => referral.Name).ThenBy(referral => referral.Id),
                ("LinkOpenCount", true) => query.OrderByDescending(referral => referral.LinkOpenCount).ThenByDescending(referral => referral.Id),
                ("LinkOpenCount", false) => query.OrderBy(referral => referral.LinkOpenCount).ThenBy(referral => referral.Id),
                ("DonationCount", true) => query.OrderByDescending(referral => referral.Donations.Count()).ThenByDescending(referral => referral.Id),
                ("DonationCount", false) => query.OrderBy(referral => referral.Donations.Count()).ThenBy(referral => referral.Id),
                ("CreateDate", false) => query.OrderBy(referral => referral.CreateDate).ThenBy(referral => referral.Id),
                _ => query.OrderByDescending(referral => referral.CreateDate).ThenByDescending(referral => referral.Id),
            };
        }

        /// <summary>
        /// Referral information shown in the administrator list.
        /// </summary>
        public sealed class ReferralListItem
        {
            /// <summary>Gets or sets the referral identifier.</summary>
            public int Id { get; set; }

            /// <summary>Gets or sets the referral code.</summary>
            public string Code { get; set; }

            /// <summary>Gets or sets the referral name.</summary>
            public string Name { get; set; }

            /// <summary>Gets or sets the creation date.</summary>
            public DateTime CreateDate { get; set; }

            /// <summary>Gets or sets the number of link opens.</summary>
            public int LinkOpenCount { get; set; }

            /// <summary>Gets or sets the number of donations.</summary>
            public int DonationCount { get; set; }
        }
    }
}
