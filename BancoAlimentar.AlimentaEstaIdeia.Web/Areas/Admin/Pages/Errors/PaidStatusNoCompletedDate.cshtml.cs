// -----------------------------------------------------------------------
// <copyright file="PaidStatusNoCompletedDate.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Paid donations with no payment completed date.
    /// </summary>
    public class PaidStatusNoCompletedDateModel : PageModel
    {
        private readonly AdminDonationErrorQueryService queryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaidStatusNoCompletedDateModel"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public PaidStatusNoCompletedDateModel(ApplicationDbContext dbContext)
        {
            this.queryService = new AdminDonationErrorQueryService(dbContext);
        }

        /// <summary>
        /// Gets the SQL executed by this check.
        /// </summary>
        public string SqlQuery => AdminErrorSqlQueries.PaidStatusNoCompletedDate;

        /// <summary>
        /// Gets matching rows.
        /// </summary>
        public IList<DonationPaymentIssueRow> Rows { get; private set; } = new List<DonationPaymentIssueRow>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            Rows = await this.queryService.GetPaidStatusNoCompletedDateAsync();
        }
    }
}
