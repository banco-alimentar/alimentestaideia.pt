// -----------------------------------------------------------------------
// <copyright file="CampaignEdit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
#pragma warning disable SA1027 // Use tabs correctly
	using System.Threading.Tasks;
	using BancoAlimentar.AlimentaEstaIdeia.Model;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.AspNetCore.Mvc.RazorPages;
	using Microsoft.EntityFrameworkCore;

	/// <summary>
	/// Edit Referral campaign details.
	/// </summary>
	public class CampaignEditModel : PageModel
    {
		private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignEditModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
		public CampaignEditModel(ApplicationDbContext context)
        {
			this.context = context;
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

			return Page();
		}
	}
}
#pragma warning restore SA1027 // Use tabs correctly