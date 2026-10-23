// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Lists donations associated with a referral.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private const int PageSize = 25;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Application database context.</param>
        public DetailsModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>Gets the referral being viewed.</summary>
        public Referral Referral { get; private set; }

        /// <summary>Gets the donations on the current page.</summary>
        public IList<Donation> Donations { get; private set; } = new List<Donation>();

        /// <summary>Gets or sets the referral ID.</summary>
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        /// <summary>Gets or sets the current page number.</summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>Gets the total number of donations for this referral.</summary>
        public int TotalCount { get; private set; }

        /// <summary>Gets the total number of pages.</summary>
        public int TotalPages { get; private set; }

        /// <summary>Gets a value indicating whether a previous page exists.</summary>
        public bool HasPreviousPage => this.PageIndex > 1;

        /// <summary>Gets a value indicating whether a next page exists.</summary>
        public bool HasNextPage => this.PageIndex < this.TotalPages;

        /// <summary>
        /// Loads the referral and its donations.
        /// </summary>
        /// <param name="id">Referral identifier.</param>
        /// <returns>The page or a not-found result.</returns>
        public async Task<IActionResult> OnGetAsync(int id)
        {
            this.Id = id;
            this.Referral = await this.context.Referrals
                .AsNoTracking()
                .FirstOrDefaultAsync(referral => referral.Id == id);
            if (this.Referral == null)
            {
                return this.NotFound();
            }

            if (this.PageIndex < 1)
            {
                this.PageIndex = 1;
            }

            IQueryable<Donation> query = this.context.Donations
                .AsNoTracking()
                .Where(donation => donation.ReferralEntity != null && donation.ReferralEntity.Id == id);

            this.TotalCount = await query.CountAsync();
            this.TotalPages = this.TotalCount == 0 ? 0 : (int)Math.Ceiling(this.TotalCount / (double)PageSize);
            if (this.TotalPages > 0 && this.PageIndex > this.TotalPages)
            {
                this.PageIndex = this.TotalPages;
            }

            this.Donations = await query
                .Include(donation => donation.User)
                .OrderByDescending(donation => donation.DonationDate)
                .ThenByDescending(donation => donation.Id)
                .Skip((this.PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return this.Page();
        }
    }
}
