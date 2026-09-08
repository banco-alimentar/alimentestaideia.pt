// -----------------------------------------------------------------------
// <copyright file="Communications.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Displays email communications sent to the signed-in user.
    /// </summary>
    public class CommunicationsModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationsModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="context">Application database context.</param>
        public CommunicationsModel(UserManager<WebUser> userManager, ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.context = context;
        }

        /// <summary>
        /// Gets the communications sent to the signed-in user.
        /// </summary>
        public IList<EmailCommunication> Communications { get; private set; } = new List<EmailCommunication>();

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

            this.Communications = await this.context.EmailCommunications
                .AsNoTracking()
                .Include(communication => communication.Payment)
                .ThenInclude(payment => payment.Donation)
                .Where(communication => communication.UserId == user.Id)
                .OrderByDescending(communication => communication.SentAtUtc)
                .ThenByDescending(communication => communication.Id)
                .ToListAsync();

            return this.Page();
        }
    }
}
