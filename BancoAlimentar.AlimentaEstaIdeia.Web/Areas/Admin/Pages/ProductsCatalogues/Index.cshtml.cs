// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.ProductsCatalogues;

using System.Collections.Generic;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// List all the product catalogue model.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IUnitOfWork context;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    public IndexModel(IUnitOfWork context)
    {
        this.context = context;
    }

    /// <summary>
    /// Gets or sets the list of product catalogue.
    /// </summary>
    public IList<ProductCatalogue> ProductCatalogue { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    public void OnGet()
    {
        ProductCatalogue = this.context.ProductCatalogue.GetAllWithCampaign();
    }
}
