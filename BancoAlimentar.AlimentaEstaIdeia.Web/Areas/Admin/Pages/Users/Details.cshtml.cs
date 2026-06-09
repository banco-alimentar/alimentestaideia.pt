// -----------------------------------------------------------------------
// <copyright file="Details.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Users
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Admin user details page.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsModel"/> class.
        /// </summary>
        /// <param name="context">Application Db Context.</param>
        public DetailsModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets or sets the donor user.
        /// </summary>
        public WebUser DonorUser { get; set; }

        /// <summary>
        /// Gets or sets the roles assigned to the user.
        /// </summary>
        public IList<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the donations made by the user.
        /// </summary>
        public IList<Donation> Donations { get; set; } = new List<Donation>();

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            DonorUser = await context.Users
                .AsNoTracking()
                .Include(user => user.Address)
                .FirstOrDefaultAsync(user => user.Id == id);

            if (DonorUser == null)
            {
                return NotFound();
            }

            Roles = await (
                from userRole in context.UserRoles.AsNoTracking()
                join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userRole.UserId == id
                orderby role.Name
                select role.Name)
                .ToListAsync();

            Donations = await context.Donations
                .AsNoTracking()
                .Include(donation => donation.ReferralEntity)
                .Include(donation => donation.ConfirmedPayment)
                .Where(donation => EF.Property<string>(donation, "UserId") == id)
                .OrderByDescending(donation => donation.DonationDate)
                .ThenByDescending(donation => donation.Id)
                .ToListAsync();

            return Page();
        }

        /// <summary>
        /// Gets the payment type label for display.
        /// </summary>
        /// <param name="donation">The donation.</param>
        /// <returns>The payment type name.</returns>
        public string GetPaymentTypeName(Donation donation)
        {
            if (donation?.ConfirmedPayment == null)
            {
                return null;
            }

            return GetPaymentTypeName(donation.ConfirmedPayment);
        }

        /// <summary>
        /// Gets the payment type label for display.
        /// </summary>
        /// <param name="payment">The payment.</param>
        /// <returns>The payment type name.</returns>
        public string GetPaymentTypeName(BasePayment payment)
        {
            return payment switch
            {
                MultiBankPayment => "MultiBank",
                CreditCardPayment => "CreditCard",
                MBWayPayment => "MBWay",
                PayPalPayment => "PayPal",
                _ => payment.GetType().Name,
            };
        }
    }
}
