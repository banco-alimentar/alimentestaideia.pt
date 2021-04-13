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

    public class CreateModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        public CreateModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        [BindProperty]
        public List<SelectListItem> Campaigns { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Campaigns = new List<SelectListItem>();
            foreach (var item in await context.Campaigns.ToListAsync())
            {
                Campaigns.Add(new SelectListItem() { Value = item.Id.ToString(), Text = item.Number });
            }

            return Page();
        }

        [BindProperty]
        public ProductCatalogue ProductCatalogue { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            ProductCatalogue.Campaign = context.Campaigns.Where(p => p.Id == ProductCatalogue.Campaign.Id).FirstOrDefault();

            context.ProductCatalogues.Add(ProductCatalogue);
            await context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
