// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
/// Display the details of the campaign.
/// </summary>
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="context">Application Db Context.</param>
    public DetailsModel(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Gets or sets the current campaign.
    /// </summary>
    public Campaign Campaign { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <param name="id">The campaign id to show details.</param>
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
}
