﻿namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.RazorPages;    

    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;

        public IndexModel(
            IUnitOfWork context,
            SignInManager<WebUser> signInManager)
        {
            this.context = context;
            this.signInManager = signInManager;
        }

        public List<TotalDonationsResult> TotalDonations { get; set; }

        public IReadOnlyList<ProductCatalogue> ProductCatalogue { get; set; }

        public bool IsUserLoggedIn { get; set; }

        public void OnGet()
        {
            LoadData();
        }

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