// -----------------------------------------------------------------------
// <copyright file="BigDonations.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Top large paid donations after 2024-11-28.
    /// </summary>
    public class BigDonationsModel : PageModel
    {
        private readonly AdminDonationErrorQueryService queryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDonationsModel"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public BigDonationsModel(ApplicationDbContext dbContext)
        {
            this.queryService = new AdminDonationErrorQueryService(dbContext);
        }

        /// <summary>
        /// Gets the SQL executed by this check.
        /// </summary>
        public string SqlQuery => AdminErrorSqlQueries.BigDonations;

        /// <summary>
        /// Gets matching donations.
        /// </summary>
        public IList<BigDonationRow> Rows { get; private set; } = new List<BigDonationRow>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            Rows = await this.queryService.GetBigDonationsAsync();
        }
    }
}
