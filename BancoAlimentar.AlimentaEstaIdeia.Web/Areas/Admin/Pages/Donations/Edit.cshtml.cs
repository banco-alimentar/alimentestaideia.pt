// -----------------------------------------------------------------------
// <copyright file="Edit.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Edit a donation.
    /// </summary>
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditModel"/> class.
        /// </summary>
        /// <param name="context">Application Db Context.</param>
        public EditModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the allow-listed donation fields submitted by the form.
        /// </summary>
        [BindProperty]
        public AdminDonationEditInput Input { get; set; }

        /// <summary>
        /// Gets the donation amount (read-only; not bindable).
        /// </summary>
        public double DonationAmount { get; private set; }

        /// <summary>
        /// Gets the payment status (read-only; not bindable).
        /// </summary>
        public PaymentStatus PaymentStatus { get; private set; }

        /// <summary>
        /// Gets the public donation identifier (read-only; not bindable).
        /// </summary>
        public Guid PublicId { get; private set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The donation id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await this.context.Donations.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (donation == null)
            {
                return NotFound();
            }

            this.PopulatePage(donation);
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
                await this.PopulateReadOnlyFieldsAsync(this.Input.Id);
                return Page();
            }

            var donation = await this.context.Donations.FirstOrDefaultAsync(d => d.Id == this.Input.Id);
            if (donation == null)
            {
                return NotFound();
            }

            this.Input.ApplyTo(donation);

            try
            {
                await this.context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!this.DonationExists(this.Input.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToPage("./Index");
        }

        private void PopulatePage(Donation donation)
        {
            this.Input = AdminDonationEditInput.FromDonation(donation);
            this.DonationAmount = donation.DonationAmount;
            this.PaymentStatus = donation.PaymentStatus;
            this.PublicId = donation.PublicId;
        }

        private async Task PopulateReadOnlyFieldsAsync(int id)
        {
            var donation = await this.context.Donations.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donation != null)
            {
                this.DonationAmount = donation.DonationAmount;
                this.PaymentStatus = donation.PaymentStatus;
                this.PublicId = donation.PublicId;
            }
        }

        private bool DonationExists(int id)
        {
            return this.context.Donations.Any(e => e.Id == id);
        }
    }
}
