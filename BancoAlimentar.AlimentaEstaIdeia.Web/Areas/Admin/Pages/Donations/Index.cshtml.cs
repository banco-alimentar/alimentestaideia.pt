// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// List all donations model.
    /// </summary>
    public class IndexModel : PageModel
    {
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
        /// Gets or sets the list of donations.
        /// </summary>
        public IList<Donation> Donation { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            Donation = await this.context.Donation.GetAll().ToListAsync();
        }
    }
}
