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

    public class ShowModel : PageModel
    {
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowModel"/> class.
        /// </summary>
        /// <param name="context"></param>
        public ShowModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public Campaign CurrentCampaign { get; set; }

        public DateTime CurrentDateTime { get; set; }

        public void OnGet()
        {
            CurrentDateTime = DateTime.Now;
            this.CurrentCampaign = this.context.CampaignRepository.GetCurrentCampaign();
        }
    }
}
