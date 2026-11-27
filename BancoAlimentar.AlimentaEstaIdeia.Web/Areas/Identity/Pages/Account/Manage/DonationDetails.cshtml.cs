// -----------------------------------------------------------------------
// <copyright file="DonationDetails.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Displays details for a donation owned by the signed-in user.
    /// </summary>
    public class DonationDetailsModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationDetailsModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Application database context.</param>
        public DonationDetailsModel(UserManager<WebUser> userManager, ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        /// <summary>
        /// Gets the donation to display.
        /// </summary>
        public Donation Donation { get; private set; }

        /// <summary>
        /// Loads the requested donation if it belongs to the signed-in user.
        /// </summary>
        /// <param name="publicId">The donation's public identifier.</param>
        /// <returns>The page or a not found result.</returns>
        public async Task<IActionResult> OnGetAsync(Guid publicId)
        {
            WebUser user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound();
            }

            this.Donation = await this.context.Donations
                .AsNoTracking()
                .Include(donation => donation.FoodBank)
                .SingleOrDefaultAsync(donation => donation.PublicId == publicId && donation.User.Id == user.Id);

            return this.Donation == null ? this.NotFound() : this.Page();
        }
    }
}
