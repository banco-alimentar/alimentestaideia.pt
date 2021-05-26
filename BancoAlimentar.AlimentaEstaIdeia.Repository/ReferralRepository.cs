// -----------------------------------------------------------------------
// <copyright file="ReferralRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default implementation for the <see cref="Referral"/> repository pattern.
    /// </summary>
    public class ReferralRepository : GenericRepository<Referral>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public ReferralRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Get all the Referrals for user.
        /// </summary>
        /// <param name="userId">A reference to the user id.</param>
        /// <returns>A <see cref="List{Referral}"/> of referrals.</returns>
        public List<Referral> GetUserReferrals(string userId)
        {
            return this.DbContext.Referrals
                .Where(r => r.User.Id == userId)
                .Include(r => r.User)
                .Include(r => r.Donations)
                .ThenInclude(d => d.DonationItems)
                .ThenInclude(i => i.ProductCatalogue)
                .ToList();
        }

        /// <summary>
        /// Get thye top campaigns that had donations in the last days.
        /// </summary>
        /// <param name="quantity">Size of the list to be returned.</param>
        /// <param name="daysToEvaluate">Max number of days from the last donation.</param>
        /// <returns>A <see cref="List{Referral}"/> of top referrals.</returns>
        public List<Referral> GetTopList(int quantity, int daysToEvaluate)
        {
            return this.DbContext.Referrals
                .Where(r => r.Active)

                // Filter only for compains that have any donation done in the last daysToEvaluate.
                .Where(r => r.Donations.Any(d => d.DonationDate > DateTime.UtcNow.AddDays(-1 * daysToEvaluate)))
                .OrderByDescending(x =>
                x.Donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .Sum(d => d.DonationAmount))
                .Take(quantity)
                .Include(r => r.Donations)
                .ThenInclude(d => d.DonationItems)
                .ThenInclude(i => i.ProductCatalogue)
                .ToList();
        }

        /// <summary>
        /// Get full referral.
        /// </summary>
        /// <param name="userId">A reference to the user id.</param>
        /// <param name="referralId">The id of the referral.</param>
        /// <returns>A <see cref="Referral"/> entity. </returns>
        public Referral GetFullReferral(string userId, int referralId)
        {
            return this.DbContext.Referrals
                .Where(r => r.User.Id == userId && r.Id == referralId)
                .Include(r => r.User)
                .Include(r => r.Donations)
                .ThenInclude(d => d.DonationItems)
                .ThenInclude(i => i.ProductCatalogue)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets an active referral by code.
        /// </summary>
        /// <param name="code">The referral code to evaluate.</param>
        /// <returns>A <see cref="Referral"/> entity. </returns>
        public Referral GetActiveCampaignsByCode(string code)
        {
            Referral referral = this.GetByCode(code);
            if (referral != null && referral.Active)
            {
                return referral;
            }

            return null;
        }

        /// <summary>
        /// Gets a referral by code.
        /// </summary>
        /// <param name="code">The referral code to evaluate.</param>
        /// <param name="userId">Optional, the user id the campaign belongs to.
        /// If not provided, all campaigns matching the code are returned.</param>
        /// <returns>A <see cref="Referral"/> entity. </returns>
        public Referral GetByCode(string code, string userId = null)
        {
            code = code.ToLowerInvariant();

            if (string.IsNullOrEmpty(userId))
            {
                return this.DbContext.Referrals.FirstOrDefault(r => r.Code == code);
            }

            return this.DbContext.Referrals
                .Where(r => r.User.Id == userId && r.Code == code)
                .FirstOrDefault();
        }

        /// <summary>
        /// Updates the state of the <c>Referral</c>. No check on the previous state is performed.
        /// </summary>
        /// <param name="referral">The referral to be updated.</param>
        /// <param name="active">The new state.</param>
        /// <returns>The updated referral.</returns>
        public Referral UpdateState(Referral referral, bool active)
        {
            referral.Active = active;
            var rows = this.DbContext.SaveChanges();
            if (rows != 1)
            {
                throw new InvalidOperationException($"Unexpected number of rows changed {rows} instead of 1.");
            }

            return referral;
        }
    }
}
