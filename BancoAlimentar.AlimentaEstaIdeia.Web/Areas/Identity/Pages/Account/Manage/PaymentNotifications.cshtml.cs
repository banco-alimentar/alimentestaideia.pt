// -----------------------------------------------------------------------
// <copyright file="PaymentNotifications.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Displays payment notification emails sent to the signed-in user.
    /// </summary>
    public class PaymentNotificationsModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentNotificationsModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Application database context.</param>
        public PaymentNotificationsModel(UserManager<WebUser> userManager, ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        /// <summary>
        /// Gets the payment notifications sent to the user.
        /// </summary>
        public IList<PaymentNotifications> Notifications { get; private set; } = new List<PaymentNotifications>();

        /// <summary>
        /// Executes the get operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            WebUser user = await this.userManager.GetUserAsync(this.User);
            if (user == null)
            {
                return this.NotFound();
            }

            this.Notifications = await this.context.PaymentNotifications
                .AsNoTracking()
                .Include(notification => notification.Payment)
                .ThenInclude(payment => payment.Donation)
                .Where(notification => EF.Property<string>(notification, "UserId") == user.Id)
                .OrderByDescending(notification => notification.Created)
                .ThenByDescending(notification => notification.Id)
                .ToListAsync();

            return this.Page();
        }
    }
}
