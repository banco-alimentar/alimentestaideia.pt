// -----------------------------------------------------------------------
// <copyright file="DonationHistory.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
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
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// 
    /// </summary>
    public class CampaigsHistoryModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;

        public List<Referral> Referrals { get; set; }


        public CampaigsHistoryModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        public async Task OnGet()
        {
                var user = await userManager.GetUserAsync(User);
            Referrals = this.context.ReferralRepository.GetUserReferrals(user.Id);
        }

        public async Task OnPost(string code)
        {
            var user = await userManager.GetUserAsync(User);

            var referral = new Referral()
            {
                User = user,
                Code = code,
                Active = true,
            };

            this.context.ReferralRepository.Add(referral);
            this.context.Complete();
            Referrals = this.context.ReferralRepository.GetUserReferrals(user.Id);
        }
    }
}
