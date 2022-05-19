// -----------------------------------------------------------------------
// <copyright file="Delete.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Campaigns;

using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Delete a campaign.
/// </summary>
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteModel"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public DeleteModel(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Gets or sets the current campaign.
    /// </summary>
    [BindProperty]
    public Campaign Campaign { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <param name="id">The campaign id to be deleted.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Campaign = await context.Campaigns.FirstOrDefaultAsync(m => m.Id == id);

        if (Campaign == null)
        {
            return NotFound();
        }

        return Page();
    }

    /// <summary>
    /// Execute the post operation.
    /// </summary>
    /// <param name="id">The campaign id to be deleted.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Campaign = await context.Campaigns.FindAsync(id);

        if (Campaign != null)
        {
            context.Campaigns.Remove(Campaign);
            await context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
