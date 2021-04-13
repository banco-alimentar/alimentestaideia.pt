namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FoodBanks
{
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    public class EditModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        public EditModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        [BindProperty]
        public FoodBank FoodBank { get; set; }

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

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            context.Attach(FoodBank).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FoodBankExists(FoodBank.Id))
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

        private bool FoodBankExists(int id)
        {
            return context.FoodBanks.Any(e => e.Id == id);
        }
    }
}
