// -----------------------------------------------------------------------
// <copyright file="Referrals.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.FeatureManagement.Mvc;

    /// <summary>
    ///
    /// </summary>
    [FeatureGate(DevelopingFeatureFlags.ReferralCampaignManagement)]
    public class ReferralsModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Top referalls in the last 90 Days ordered by total donated.
        /// </summary>
        public List<Referral> Referrals { get; set; }

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
        /// Gets the referal list page.
        /// </summary>
        /// <returns>Gets the referrals list page.</returns>
        public async Task OnGet()
        {
            Referrals = this.context.ReferralRepository.GetTopList(10, 30);
        }
    }
}
