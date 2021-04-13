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

    public class EditModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        public EditModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        [BindProperty]
        public ProductCatalogue ProductCatalogue { get; set; }

        [BindProperty]
        public List<SelectListItem> Campaigns { get; set; }

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

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
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
