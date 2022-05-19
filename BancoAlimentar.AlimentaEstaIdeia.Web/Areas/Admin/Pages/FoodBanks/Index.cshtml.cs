// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.FoodBanks;

using System.Collections.Generic;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// List all the food bank model.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    public IndexModel(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Gets or sets the list of food bank.
    /// </summary>
    public IList<FoodBank> FoodBank { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public async Task OnGetAsync()
    {
        FoodBank = await context.FoodBanks.ToListAsync();
    }
}
