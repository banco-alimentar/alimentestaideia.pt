// -----------------------------------------------------------------------
// <copyright file="SubscriptionNotificationRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Subscription notification system.
    /// </summary>
    public class SubscriptionNotificationRepository : GenericRepository<Subscription, ApplicationDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionNotificationRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public SubscriptionNotificationRepository(
            ApplicationDbContext context,
            IMemoryCache memoryCache,
            TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Search for an existing notification for the subscription.
        /// </summary>
        /// <param name="subscriptionId">Subscription id.</param>
        /// <returns>True if the notification has been sent, false otherwise.</returns>
        public bool FindNotificationForSubscription(int subscriptionId)
        {
            return this.DbContext.SubscriptionNotifications
                .Where(p => p.Subscription.Id == subscriptionId)
                .FirstOrDefault() != null;
        }

        /// <summary>
        /// Adds a new notification for the subscription.
        /// </summary>
        /// <param name="value">A reference to the <see cref="Subscription"/>.</param>
        public void AddNotification(Subscription value)
        {
            this.DbContext.SubscriptionNotifications.Add(new SubscriptionNotification()
            {
                Subscription = value,
                Created = DateTime.UtcNow,
            });

            this.DbContext.SaveChanges();
        }
    }
}
