using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BancoAlimentar.AlimentaEstaIdeia.Model;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    public class DeleteModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext _context;

        public DeleteModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Donation Donation { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Donation = await _context.Donations.FirstOrDefaultAsync(m => m.Id == id);

            if (Donation == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Donation = await _context.Donations.FindAsync(id);

            if (Donation != null)
            {
                _context.Donations.Remove(Donation);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
