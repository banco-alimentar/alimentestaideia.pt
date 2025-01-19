// -----------------------------------------------------------------------
// <copyright file="CampaignEdit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Edit Referral campaign details.
    /// </summary>
    public class CampaignEditModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<WebUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignEditModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="userManager">User Manager.</param>
        public CampaignEditModel(ApplicationDbContext context, UserManager<WebUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        /// <summary>
        /// Gets or sets the current referral.
        /// </summary>
        [BindProperty]
        public Referral Referral { get; set; }

        /// <summary>
        /// Executes the get operation.
        /// </summary>
        /// <param name="id">Referral id.</param>
        /// <returns>Task.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            this.Referral = await context.Referrals.FirstOrDefaultAsync(m => m.Id == id);

            if (this.Referral == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            if (!(user != null && userManager != null && user.Id == Referral.User?.Id))
            {
                return NotFound();
            }

            return Page();
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            context.Attach(Referral).State = EntityState.Modified;

            await context.SaveChangesAsync();

            return RedirectToPage("./CampaignsHistory");
        }
    }
}