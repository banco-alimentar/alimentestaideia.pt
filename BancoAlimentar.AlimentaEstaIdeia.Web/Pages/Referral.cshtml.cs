// -----------------------------------------------------------------------
// <copyright file="Referral.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// The referal redirection page.
    /// </summary>
    public class ReferralModel : PageModel
    {
        private readonly IUnitOfWork context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        public ReferralModel(IUnitOfWork context)
        {
            this.context = context;
        }

        /// <summary>
        /// Add cookie and redirect to donations.
        /// </summary>
        /// <param name="text">The referral code.</param>
        /// <returns>Redirect to the donations page.</returns>
        public ActionResult OnGet(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // Check if referral code exists. Give error if it does not. Redirect if it does exist.
                var referral = this.context.ReferralRepository.GetActiveCampaignsByCode(text);
                if (referral != null)
                {
                    this.HttpContext.Session.SetString("Referral", text);
                    return this.RedirectToPage("./Donation");
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return this.RedirectToPage("./Donation");
            }
        }
    }
}
