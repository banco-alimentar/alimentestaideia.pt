// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
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
        /// Gets or sets the total donations.
        /// </summary>
        public List<TotalDonationsResult> TotalDonations { get; set; }

        /// <summary>
        /// Gets or sets the product catalogue.
        /// </summary>
        public IReadOnlyList<ProductCatalogue> ProductCatalogue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is logged in.
        /// </summary>
        public bool IsUserLoggedIn { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
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
            ProductCatalogue = this.context.ProductCatalogue.GetCurrentProductCatalogue();
            TotalDonations = this.context.Donation.GetTotalDonations(this.ProductCatalogue);

            IsUserLoggedIn = signInManager.IsSignedIn(new ClaimsPrincipal(User.Identity));

            ViewData["IsPostBack"] = false;
            ViewData["HasReference"] = false;
            ViewData["IsMultibanco"] = false;
        }
    }
}
