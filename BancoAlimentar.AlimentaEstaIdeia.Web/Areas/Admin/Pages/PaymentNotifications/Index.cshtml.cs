// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.PaymentNotifications
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
    /// Lists payment notification emails sent by the application.
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
        /// Gets the notifications on the current page.
        /// </summary>
        public IList<PaymentNotifications> Notifications { get; private set; } = new List<PaymentNotifications>();

        /// <summary>
        /// Gets the total number of notifications.
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
        /// Executes the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            if (this.PageIndex < 1)
            {
                this.PageIndex = 1;
            }

            IQueryable<PaymentNotifications> query = this.context.PaymentNotifications.AsNoTracking();
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

            this.Notifications = await query
                .Include(notification => notification.User)
                .Include(notification => notification.Payment)
                .ThenInclude(payment => payment.Donation)
                .OrderByDescending(notification => notification.Created)
                .ThenByDescending(notification => notification.Id)
                .Skip((this.PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}
