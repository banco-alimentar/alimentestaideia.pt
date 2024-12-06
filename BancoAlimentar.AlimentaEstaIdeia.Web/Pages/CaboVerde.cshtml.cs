// -----------------------------------------------------------------------
// <copyright file="CaboVerde.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Index page model.
    /// </summary>
    public class CaboVerdeModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaboVerdeModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="signInManager">Sign in manager.</param>
        public CaboVerdeModel(
            IUnitOfWork context,
            SignInManager<WebUser> signInManager)
        {
            this.context = context;
            this.signInManager = signInManager;
        }

        /// <summary>
        /// Gets or sets the total donations.
        /// </summary>
        public List<TotalDonationsResult> TotalDonations { get; set; }

        /// <summary>
        /// Gets or sets the product catalogue.
        /// </summary>
        public IReadOnlyList<ProductCatalogue> ProductCatalogue { get; set; }

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
            this.HttpContext.Session.SetInt32("FoodBankIdContext", 22);

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
            ProductCatalogue = this.context.ProductCatalogue.GetCurrentProductCatalogue().ProductCatalogues;
            TotalDonations = this.context.Donation.GetTotalDonationsOfFoodBank(this.ProductCatalogue, 22);
            CampaignStartDateString = this.context.CampaignRepository.GetCurrentCampaign().Start.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

            IsUserLoggedIn = signInManager.IsSignedIn(new ClaimsPrincipal(User.Identity));

            ViewData["IsPostBack"] = false;
            ViewData["HasReference"] = false;
            ViewData["IsMultibanco"] = false;
        }
    }
}
