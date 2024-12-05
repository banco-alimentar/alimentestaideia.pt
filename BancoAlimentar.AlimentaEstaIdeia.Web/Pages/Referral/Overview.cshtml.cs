// -----------------------------------------------------------------------
// <copyright file="Overview.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Referral
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Referral;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// This page show the status of the referral.
    /// </summary>
    public class OverviewModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverviewModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Unit of work.</param>
        public OverviewModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
            TotalAmount = new List<OverviewProductCatalogItem>();
        }

        /// <summary>
        /// Gets or sets the total donation for the referral.
        /// </summary>
        public int TotalDonations { get; set; }

        /// <summary>
        /// Gets or sets the total amounts for the product catalog.
        /// </summary>
        public List<OverviewProductCatalogItem> TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the The referral information.
        /// </summary>
        public Referral Referral { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="nameOfTheReferral">Name of the referral.</param>
        public IActionResult OnGet(string nameOfTheReferral)
        {
            if (!string.IsNullOrEmpty(nameOfTheReferral))
            {
                Referral referral = this.context.ReferralRepository.GetCampaignsByCode(nameOfTheReferral);
                if (referral == null)
                {
                    return NotFound();
                }
                else
                {
                    if (string.IsNullOrEmpty(referral.Name))
                    {
                        referral.Name = nameOfTheReferral;
                    }
                }

                this.Referral = referral;

                List<Donation> donations = this.context.ReferralRepository.GetPaidDonationsByReferralCode(nameOfTheReferral);
                IReadOnlyList<ProductCatalogue> productCatalogues = this.context.ProductCatalogue.GetCurrentProductCatalogue().ProductCatalogues;
                List<DonationItem> allDonations = new List<DonationItem>();
                foreach (var donationItemList in donations.Select(p => p.DonationItems))
                {
                    allDonations.AddRange(donationItemList.ToList());
                }

                TotalDonations = donations.Count;
                foreach (var item in productCatalogues)
                {
                    var all = allDonations.Where(p => p.ProductCatalogue.Name == item.Name).Select(p => p);
                    TotalAmount.Add(new OverviewProductCatalogItem()
                    {
                        Name = item.Name,
                        Unit = item.UnitOfMeasure,
                        Value = all.Sum(p => p.Quantity * p.Price),
                    });
                }

                return Page();
            }
            else
            {
                return RedirectToPage("./");
            }
        }
    }
}
