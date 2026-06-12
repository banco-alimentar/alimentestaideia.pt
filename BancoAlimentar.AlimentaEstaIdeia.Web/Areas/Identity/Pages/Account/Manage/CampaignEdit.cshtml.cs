// -----------------------------------------------------------------------
// <copyright file="CampaignEdit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Edit Referral campaign details.
    /// </summary>
    [RequestSizeLimit(3 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
    public class CampaignEditModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<WebUser> userManager;
        private readonly ReferralImageService referralImageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignEditModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="referralImageService">Referral image storage service.</param>
        public CampaignEditModel(
            ApplicationDbContext context,
            UserManager<WebUser> userManager,
            ReferralImageService referralImageService)
        {
            this.context = context;
            this.userManager = userManager;
            this.referralImageService = referralImageService;
        }

        /// <summary>
        /// Gets or sets the current referral.
        /// </summary>
        [BindProperty]
        public Referral Referral { get; set; }

        /// <summary>
        /// Gets or sets the uploaded referral image.
        /// </summary>
        [BindProperty]
        public IFormFile ImageUpload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current image should be removed.
        /// </summary>
        [BindProperty]
        public bool RemoveImage { get; set; }

        /// <summary>
        /// Gets the browser-ready URL for the current referral image.
        /// </summary>
        public string ReferralImageDisplayUrl { get; private set; }

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

            this.Referral = await context.Referrals
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (this.Referral == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            if (!(user != null && userManager != null && user.Id == Referral.User?.Id))
            {
                return NotFound();
            }

            this.ReferralImageDisplayUrl = this.referralImageService.ResolveUrl(this.Referral.ImageUrl);
            return Page();
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            var existingReferral = await context.Referrals
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == Referral.Id);

            if (existingReferral == null || user == null || existingReferral.User?.Id != user.Id)
            {
                return NotFound();
            }

            existingReferral.Name = Referral.Name;
            existingReferral.Active = Referral.Active;
            existingReferral.IsPublic = Referral.IsPublic;

            try
            {
                if (this.RemoveImage)
                {
                    await this.referralImageService.DeleteAsync(existingReferral.ImageUrl);
                    existingReferral.ImageUrl = null;
                }
                else if (this.ImageUpload != null && this.ImageUpload.Length > 0)
                {
                    await this.referralImageService.DeleteAsync(existingReferral.ImageUrl);
                    existingReferral.ImageUrl = await this.referralImageService.UploadAsync(existingReferral.Id, this.ImageUpload);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                this.Referral = existingReferral;
                this.ReferralImageDisplayUrl = this.referralImageService.ResolveUrl(existingReferral.ImageUrl);
                return Page();
            }

            await context.SaveChangesAsync();
            return RedirectToPage("./CampaignsHistory");
        }
    }
}
