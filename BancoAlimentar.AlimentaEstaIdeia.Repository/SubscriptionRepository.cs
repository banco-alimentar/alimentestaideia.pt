// -----------------------------------------------------------------------
// <copyright file="SubscriptionRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Default implementation for the <see cref="SubscriptionRepository"/> repository pattern.
    /// </summary>
    public class SubscriptionRepository : GenericRepository<Subscription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public SubscriptionRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Create a subscription.
        /// </summary>
        /// <param name="donation">Initial <see cref="Donation"/> that trigger the subscription.</param>
        /// <param name="transactionKey">Transaction key.</param>
        /// <param name="url">Payment url.</param>
        public void CreateSubscription(Donation donation, string transactionKey, string url)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey) && !string.IsNullOrEmpty(url))
            {
                Subscription value = new Subscription()
                {
                    Created = DateTime.UtcNow,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    ExpirationTime = DateTime.UtcNow.AddYears(1),
                    EasyPayTransactionId = transactionKey,
                    Url = url,
                    InitialDonation = donation,
                };

                this.DbContext.Subscriptions.Add(value);
                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Gets a list of the <see cref="WebUser"/> subscriptions.
        /// </summary>
        /// <param name="user">A reference to the user.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="Subscription"/> for the user.</returns>
        public List<Subscription> GetUserSubscription(WebUser user)
        {
            List<Subscription> result = null;

            if (user != null)
            {
                result = this.DbContext.UsersSubscriptions
                    .Where(p => p.User.Id == user.Id)
                    .Select(p => p.Subscription)
                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// Mark a subscription as deleted.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        /// <returns>true if the operation is succeed, false otherwise.</returns>
        public bool DeleteSubscription(int subscriptionId)
        {
            bool result = false;

            Subscription value = this.DbContext.Subscriptions
                .Where(p => p.Id == subscriptionId)
                .FirstOrDefault();

            if (value != null)
            {
                value.IsDeleted = true;
                value.Status = SubscriptionStatus.Inactive;
                this.DbContext.SaveChanges();
                result = true;
            }

            return result;
        }
    }
}
