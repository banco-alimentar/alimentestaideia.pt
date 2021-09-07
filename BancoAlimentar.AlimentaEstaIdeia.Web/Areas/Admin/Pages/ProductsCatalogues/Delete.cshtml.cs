// -----------------------------------------------------------------------
// <copyright file="Delete.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.ProductsCatalogues
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Delete product catalogue model.
    /// </summary>
    public class DeleteModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public DeleteModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the product catalogue.
        /// </summary>
        [BindProperty]
        public ProductCatalogue ProductCatalogue { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">Product catalogue id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ProductCatalogue = await context.ProductCatalogues.FirstOrDefaultAsync(m => m.Id == id);

            if (ProductCatalogue == null)
            {
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="id">Product catalogue id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ProductCatalogue = await context.ProductCatalogues.FindAsync(id);

            if (ProductCatalogue != null)
            {
                context.ProductCatalogues.Remove(ProductCatalogue);
                await context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
