// -----------------------------------------------------------------------
// <copyright file="Create.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.ProductsCatalogues;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Create a new product catalogue model.
/// </summary>
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    public CreateModel(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Gets or sets or set the list of campaigns.
    /// </summary>
    [BindProperty]
    public List<SelectListItem> Campaigns { get; set; }

    /// <summary>
    /// Gets or sets the product catalogue.
    /// </summary>
    [BindProperty]
    public ProductCatalogue ProductCatalogue { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        Campaigns = new List<SelectListItem>();
        foreach (var item in await context.Campaigns.ToListAsync())
        {
            Campaigns.Add(new SelectListItem() { Value = item.Id.ToString(), Text = item.Number });
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

        ProductCatalogue.Campaign = context.Campaigns.Where(p => p.Id == ProductCatalogue.Campaign.Id).FirstOrDefault();

        context.ProductCatalogues.Add(ProductCatalogue);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
