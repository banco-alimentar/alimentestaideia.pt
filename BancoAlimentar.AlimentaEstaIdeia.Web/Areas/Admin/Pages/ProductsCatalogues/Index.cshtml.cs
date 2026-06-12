// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.ProductsCatalogues
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc;
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
        /// Gets or sets the selected campaign filter.
        /// Defaults to the most recent campaign on first visit; null means all campaigns when explicitly selected.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int? CampaignFilter { get; set; }

        /// <summary>
        /// Gets campaign options for the filter dropdown.
        /// </summary>
        public IList<Campaign> CampaignOptions { get; private set; } = new List<Campaign>();

        /// <summary>
        /// Gets or sets the list of product catalogue.
        /// </summary>
        public IList<ProductCatalogue> ProductCatalogue { get; set; } = new List<ProductCatalogue>();

        /// <summary>
        /// Gets a value indicating whether the all-campaigns option is selected.
        /// </summary>
        public bool IsAllCampaignsSelected => !this.CampaignFilter.HasValue;

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
            this.CampaignOptions = this.context.CampaignRepository.GetAll()
                .OrderByDescending(campaign => campaign.Start)
                .ThenByDescending(campaign => campaign.Id)
                .ToList();

            if (!this.Request.Query.ContainsKey(nameof(this.CampaignFilter)))
            {
                Campaign? latestCampaign = this.CampaignOptions.FirstOrDefault();
                if (latestCampaign != null)
                {
                    this.CampaignFilter = latestCampaign.Id;
                }
            }

            IList<ProductCatalogue> products = this.context.ProductCatalogue.GetAllWithCampaign();

            if (this.CampaignFilter.HasValue)
            {
                products = products
                    .Where(product => product.Campaign != null && product.Campaign.Id == this.CampaignFilter.Value)
                    .ToList();
            }

            this.ProductCatalogue = products;
        }
    }
}
