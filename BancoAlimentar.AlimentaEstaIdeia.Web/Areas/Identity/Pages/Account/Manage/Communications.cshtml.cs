// -----------------------------------------------------------------------
// <copyright file="Communications.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System;
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
        /// Gets the communications and payment notifications sent to the signed-in user.
        /// </summary>
        public IList<CommunicationEntry> Communications { get; private set; } = new List<CommunicationEntry>();

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

            var emailCommunications = await this.context.EmailCommunications
                .AsNoTracking()
                .Include(communication => communication.Payment)
                .ThenInclude(payment => payment.Donation)
                .Where(communication => communication.UserId == user.Id)
                .OrderByDescending(communication => communication.SentAtUtc)
                .ThenByDescending(communication => communication.Id)
                .ToListAsync();

            var paymentNotifications = await this.context.PaymentNotifications
                .AsNoTracking()
                .Include(notification => notification.Payment)
                .ThenInclude(payment => payment.Donation)
                .Where(notification => EF.Property<string>(notification, "UserId") == user.Id)
                .OrderByDescending(notification => notification.Created)
                .ThenByDescending(notification => notification.Id)
                .ToListAsync();

            this.Communications = emailCommunications
                .Select(communication => new CommunicationEntry
                {
                    ToAddress = communication.ToAddress,
                    SentAtUtc = communication.SentAtUtc,
                    Subject = communication.Subject,
                    PaymentId = communication.Payment?.Id,
                    DonationId = communication.Payment?.Donation?.Id,
                    DonationPublicId = communication.Payment?.Donation?.PublicId,
                })
                .Concat(paymentNotifications.Select(notification => new CommunicationEntry
                {
                    ToAddress = user.Email,
                    SentAtUtc = notification.Created,
                    Subject = notification.Subject,
                    PaymentId = notification.Payment?.Id,
                    DonationId = notification.Payment?.Donation?.Id,
                    DonationPublicId = notification.Payment?.Donation?.PublicId,
                }))
                .OrderByDescending(communication => communication.SentAtUtc)
                .ToList();

            return this.Page();
        }

        /// <summary>
        /// Represents one email communication or payment notification in the combined list.
        /// </summary>
        public class CommunicationEntry
        {
            /// <summary>
            /// Gets or sets the recipient email address.
            /// </summary>
            public string ToAddress { get; set; }

            /// <summary>
            /// Gets or sets when the message was sent.
            /// </summary>
            public DateTime SentAtUtc { get; set; }

            /// <summary>
            /// Gets or sets the message subject.
            /// </summary>
            public string Subject { get; set; }

            /// <summary>
            /// Gets or sets the payment identifier.
            /// </summary>
            public int? PaymentId { get; set; }

            /// <summary>
            /// Gets or sets the donation identifier.
            /// </summary>
            public int? DonationId { get; set; }

            /// <summary>
            /// Gets or sets the public donation identifier used by the details page.
            /// </summary>
            public Guid? DonationPublicId { get; set; }
        }
    }
}
