// -----------------------------------------------------------------------
// <copyright file="PaidStatusZeroPaidValue.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Paid donations with zero paid value on the confirmed payment.
    /// </summary>
    public class PaidStatusZeroPaidValueModel : PageModel
    {
        private const int PageSize = 25;

        private readonly AdminDonationErrorQueryService queryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaidStatusZeroPaidValueModel"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public PaidStatusZeroPaidValueModel(ApplicationDbContext dbContext)
        {
            this.queryService = new AdminDonationErrorQueryService(dbContext);
        }

        /// <summary>
        /// Gets the SQL executed by this check.
        /// </summary>
        public string SqlQuery => AdminErrorSqlQueries.PaidStatusZeroPaidValue;

        /// <summary>
        /// Gets or sets the current page index (1-based).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets the number of rows per page.
        /// </summary>
        public int RowsPerPage => PageSize;

        /// <summary>
        /// Gets the total number of matching rows.
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
        /// Gets matching rows for the current page.
        /// </summary>
        public IList<DonationPaymentIssueRow> Rows { get; private set; } = new List<DonationPaymentIssueRow>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            if (PageIndex < 1)
            {
                PageIndex = 1;
            }

            (IList<DonationPaymentIssueRow> rows, int totalCount) = await this.queryService.GetPaidStatusZeroPaidValueAsync(PageIndex, PageSize);
            TotalCount = totalCount;
            TotalPages = TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (TotalPages > 0 && PageIndex > TotalPages)
            {
                PageIndex = TotalPages;
                (rows, totalCount) = await this.queryService.GetPaidStatusZeroPaidValueAsync(PageIndex, PageSize);
                TotalCount = totalCount;
            }

            Rows = rows;
        }
    }
}
