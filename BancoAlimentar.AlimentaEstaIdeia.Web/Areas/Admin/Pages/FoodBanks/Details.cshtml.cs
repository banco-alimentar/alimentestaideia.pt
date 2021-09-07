// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FoodBanks
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Details food bank model.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public DetailsModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the food bank.
        /// </summary>
        public FoodBank FoodBank { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">Food bank id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            FoodBank = await context.FoodBanks.FirstOrDefaultAsync(m => m.Id == id);

            if (FoodBank == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
