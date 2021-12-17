// -----------------------------------------------------------------------
// <copyright file="CampaignsHistory.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Campaigns history model.
    /// </summary>
    public class CampaignsHistoryModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignsHistoryModel"/> class.
        /// </summary>
        /// <param name="userManager">UserManager.</param>
        /// <param name="context">Unit of context.</param>
        public CampaignsHistoryModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the list of Referrals.
        /// </summary>
        public List<Referral> Referrals { get; set; } = new List<Referral>();

        /// <summary>
        /// Gets or sets a value indicating whether active campaigns exits.
        /// </summary>
        public bool ActiveCampaignExists { get; set; }

        /// <summary>
        /// Executed the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGet()
        {
            var user = await userManager.GetUserAsync(User);
            Referrals = this.context.ReferralRepository.GetUserReferrals(user.Id);
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <param name="code">Referral code.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnPostAsync(string code)
        {
            code = HttpUtility.UrlEncode(code);
            var user = await userManager.GetUserAsync(User);

            // var existingReferral = context.ReferralRepository.GetByCode(code, user.Id);
            var existingReferral = context.ReferralRepository.GetByCode(code);
            if (existingReferral == null)
            {
                var referral = new Referral()
                {
                    User = user,
                    Code = code,
                    Active = true,
                };

                this.context.ReferralRepository.Add(referral);
            }
            else if (!existingReferral.Active)
            {
                context.ReferralRepository.UpdateState(existingReferral, true);
            }
            else
            {
                ActiveCampaignExists = true;
            }

            this.context.Complete();
            Referrals = this.context.ReferralRepository.GetUserReferrals(user.Id);
        }
    }
}
