// -----------------------------------------------------------------------
// <copyright file="Delete.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.CustomDonors
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Detele a donation.
    /// </summary>
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteModel"/> class.
        /// </summary>
        public DeleteModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the donation.
        /// </summary>
        [BindProperty]
        public Donation Donation { get; set; }

        /// <summary>
        /// Get the donation for delete.
        /// </summary>
        /// <param name="id">Id of the donation to delete.</param>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Donation = await context.Donations.FirstOrDefaultAsync(m => m.Id == id);

            if (Donation == null)
            {
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Delete the donation.
        /// </summary>
        /// <param name="id">Donation to delete.</param>
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Donation = await context.Donations.FindAsync(id);

            if (Donation != null)
            {
                context.Donations.Remove(Donation);
                await context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
