// -----------------------------------------------------------------------
// <copyright file="CampaignDetail.cshtml.cs" company="Federa��o Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federa��o Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Campaign details.
    /// </summary>
    public class CampaigDetailModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IHtmlLocalizer<IdentitySharedResources> localizer;


        /// <summary>
        /// Initializes a new instance of the <see cref="CampaigDetailModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Unit of work.</param>
        public CampaigDetailModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IHtmlLocalizer<IdentitySharedResources> localizer)
        {
            this.userManager = userManager;
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the Referral.
        /// </summary>
        public Referral Referral { get; set; }

        /// <summary>
        /// Gets or sets the valid list of donations.
        /// </summary>
        public List<Donation> ValidDonations { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">Referral id.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGet(int id)
        {
            var user = await userManager.GetUserAsync(User);
            Referral = this.context.ReferralRepository.GetFullReferral(user.Id, id);
            ValidDonations = Referral.Donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList();
        }
    }
}
