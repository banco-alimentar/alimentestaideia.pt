// -----------------------------------------------------------------------
// <copyright file="Edit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FoodBanks;

using System.Linq;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Edti the food bank model.
/// </summary>
public class EditModel : PageModel
{
    private readonly ApplicationDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    public EditModel(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Gets or sets the food bank.
    /// </summary>
    [BindProperty]
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
