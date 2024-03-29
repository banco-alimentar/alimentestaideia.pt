// -----------------------------------------------------------------------
// <copyright file="Referrals.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.FeatureManagement.Mvc;

    /// <summary>
    /// Referrals model.
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.ReferralCampaignManagement)]
    public class ReferralsModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralsModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="context">A reference to the <see cref="IUnitOfWork"/>.</param>
        public ReferralsModel(
            IConfiguration configuration,
            IUnitOfWork context)
        {
            this.configuration = configuration;
            this.context = context;
        }

        /// <summary>
        /// Gets or sets top referalls in the last 90 Days ordered by total donated.
        /// </summary>
        public List<AlimentaEstaIdeia.Model.Referral> Referrals { get; set; }

        /// <summary>
        /// Gets the referal list page.
        /// </summary>
        public void OnGet()
        {
            Referrals = this.context.ReferralRepository.GetTopList(10, 30);
        }
    }
}
