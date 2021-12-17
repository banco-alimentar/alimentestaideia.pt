// -----------------------------------------------------------------------
// <copyright file="Show.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Campaigns
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Show the campaign information.
    /// </summary>
    public class ShowModel : PageModel
    {
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public ShowModel(IUnitOfWork context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the current campaign.
        /// </summary>
        public Campaign CurrentCampaign { get; set; }

        /// <summary>
        /// Gets or sets the current datetime.
        /// </summary>
        public DateTime CurrentDateTime { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        public void OnGet()
        {
            CurrentDateTime = DateTime.Now;
            this.CurrentCampaign = this.context.CampaignRepository.GetCurrentCampaign();
        }
    }
}
