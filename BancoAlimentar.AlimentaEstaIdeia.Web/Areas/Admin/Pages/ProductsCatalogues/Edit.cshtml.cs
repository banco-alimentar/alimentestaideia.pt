// -----------------------------------------------------------------------
// <copyright file="Edit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.ProductsCatalogues
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Edit product catalogue model.
    /// </summary>
    public class EditModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public EditModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the product catalogue.
        /// </summary>
        [BindProperty]
        public ProductCatalogue ProductCatalogue { get; set; }

        /// <summary>
        /// Gets or sets the list of campaigns.
        /// </summary>
        [BindProperty]
        public List<SelectListItem> Campaigns { get; set; }

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

            ProductCatalogue = await context.ProductCatalogues.Include(p => p.Campaign).FirstOrDefaultAsync(m => m.Id == id);
            Campaigns = new List<SelectListItem>();
            foreach (var item in await context.Campaigns.ToListAsync())
            {
                Campaigns.Add(new SelectListItem() { Value = item.Id.ToString(), Text = item.Number });
            }

            if (ProductCatalogue == null)
            {
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            context.Attach(ProductCatalogue).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductCatalogueExists(ProductCatalogue.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ProductCatalogueExists(int id)
        {
            return context.ProductCatalogues.Any(e => e.Id == id);
        }
    }
}
