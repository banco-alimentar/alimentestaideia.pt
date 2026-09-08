// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Communications
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
    /// Lists non-payment email communications sent by the application.
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
        /// Gets or sets the current page index.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets or sets the free-text filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        /// <summary>
        /// Gets or sets the communication column used for sorting.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "SentAtUtc";

        /// <summary>
        /// Gets or sets a value indicating whether communications are sorted descending.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        /// <summary>
        /// Gets the communications on the current page.
        /// </summary>
        public IList<EmailCommunication> Communications { get; private set; } = new List<EmailCommunication>();

        /// <summary>
        /// Gets the total number of communications.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Gets the number of communications shown on each page.
        /// </summary>
        public int CommunicationsPerPage => PageSize;

        /// <summary>
        /// Gets a value indicating whether a previous page exists.
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// Gets a value indicating whether a next page exists.
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Executes the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            this.Search = this.Search?.Trim();
            if (this.PageIndex < 1)
            {
                this.PageIndex = 1;
            }

            IQueryable<EmailCommunication> query = this.context.EmailCommunications
                .AsNoTracking()
                .Where(communication => !communication.PaymentId.HasValue);
            if (!string.IsNullOrWhiteSpace(this.Search))
            {
                query = query.Where(communication =>
                    communication.FromAddress.Contains(this.Search)
                    || communication.ToAddress.Contains(this.Search)
                    || (communication.Subject != null && communication.Subject.Contains(this.Search))
                    || (communication.User != null && communication.User.Email.Contains(this.Search)));
            }

            this.SortBy = this.NormalizeSortBy(this.SortBy);
            this.TotalCount = await query.CountAsync();
            this.TotalPages = this.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(this.TotalCount / (double)PageSize);

            if (this.TotalPages > 0 && this.PageIndex > this.TotalPages)
            {
                this.PageIndex = this.TotalPages;
            }

            if (this.TotalCount == 0)
            {
                return;
            }

            query = query.Include(communication => communication.User);
            this.Communications = await this.ApplyCommunicationSort(query, this.SortBy, this.SortDescending)
                .Skip((this.PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the next sort direction when a communication column is clicked.
        /// </summary>
        /// <param name="column">The communication column.</param>
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
        /// Gets the sort direction indicator for a communication column.
        /// </summary>
        /// <param name="column">The communication column.</param>
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
                "FromAddress" => "FromAddress",
                "ToAddress" => "ToAddress",
                "Subject" => "Subject",
                "User" => "User",
                _ => "SentAtUtc",
            };
        }

        /// <summary>
        /// Applies the requested communication sort with a stable identifier tie-breaker.
        /// </summary>
        /// <param name="query">Communication query.</param>
        /// <param name="sortBy">Normalized sort column.</param>
        /// <param name="sortDescending">Whether to sort descending.</param>
        /// <returns>Sorted communication query.</returns>
        private IOrderedQueryable<EmailCommunication> ApplyCommunicationSort(
            IQueryable<EmailCommunication> query,
            string sortBy,
            bool sortDescending)
        {
            return sortBy switch
            {
                "FromAddress" => sortDescending
                    ? query.OrderByDescending(communication => communication.FromAddress).ThenByDescending(communication => communication.Id)
                    : query.OrderBy(communication => communication.FromAddress).ThenBy(communication => communication.Id),
                "ToAddress" => sortDescending
                    ? query.OrderByDescending(communication => communication.ToAddress).ThenByDescending(communication => communication.Id)
                    : query.OrderBy(communication => communication.ToAddress).ThenBy(communication => communication.Id),
                "Subject" => sortDescending
                    ? query.OrderByDescending(communication => communication.Subject).ThenByDescending(communication => communication.Id)
                    : query.OrderBy(communication => communication.Subject).ThenBy(communication => communication.Id),
                "User" => sortDescending
                    ? query.OrderByDescending(communication => communication.User == null ? null : communication.User.Email).ThenByDescending(communication => communication.Id)
                    : query.OrderBy(communication => communication.User == null ? null : communication.User.Email).ThenBy(communication => communication.Id),
                _ => sortDescending
                    ? query.OrderByDescending(communication => communication.SentAtUtc).ThenByDescending(communication => communication.Id)
                    : query.OrderBy(communication => communication.SentAtUtc).ThenBy(communication => communication.Id),
            };
        }
    }
}
