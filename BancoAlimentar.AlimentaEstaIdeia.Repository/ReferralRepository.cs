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
        /// <returns>A <see cref="List{Referral}"/> of referral.</returns>
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
        public Referral GetByCode(string code)
        {
            code = code.ToLower();
            return this.DbContext.Referrals.FirstOrDefault(r => r.Code == code && r.Active);
        }
    }
}
