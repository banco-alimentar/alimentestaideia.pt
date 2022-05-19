// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants.BancoAlimentar.Pages;

using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using global::BancoAlimentar.AlimentaEstaIdeia.Model;
using global::BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using global::BancoAlimentar.AlimentaEstaIdeia.Repository;
using global::BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Index page model.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IUnitOfWork context;
    private readonly SignInManager<WebUser> signInManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="context">Unit of work.</param>
    /// <param name="signInManager">Sign in manager.</param>
    public IndexModel(
        IUnitOfWork context,
        SignInManager<WebUser> signInManager)
    {
        this.context = context;
        this.signInManager = signInManager;
    }

    /// <summary>
    /// Gets or sets the total donation.
    /// </summary>
    public TotalDonationsResult TotalDonation { get; set; }

    /// <summary>
    /// Gets or sets the product catalogue.
    /// </summary>
    public ProductCatalogue ProductCatalogue { get; set; }

    /// <summary>
    /// Gets or sets the CampaignStartDateString.
    /// </summary>
    public string CampaignStartDateString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is logged in.
    /// </summary>
    public bool IsUserLoggedIn { get; set; }

    /// <summary>
    /// Execute the get operation.
    /// </summary>
    public void OnGet()
    {
        this.HttpContext.Session.Remove("FoodBankIdContext");
        LoadData();
    }

    /// <summary>
    /// Execute the post operation.
    /// </summary>
    public void OnPost()
    {
        LoadData();
    }

    private void LoadData()
    {
        ProductCatalogue = this.context.ProductCatalogue.GetCashProductCatalogue();
        TotalDonation = this.context.Donation.GetTotalCashDonation(ProductCatalogue);
        CampaignStartDateString = this.context.CampaignRepository.GetCurrentCampaign().Start.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        IsUserLoggedIn = signInManager.IsSignedIn(new ClaimsPrincipal(User.Identity));

        ViewData["IsPostBack"] = false;
        ViewData["HasReference"] = false;
        ViewData["IsMultibanco"] = false;
    }
}
