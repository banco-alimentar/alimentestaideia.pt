﻿namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FoodBanks
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class CreateModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        public CreateModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public FoodBank FoodBank { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            context.FoodBanks.Add(FoodBank);
            await context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}