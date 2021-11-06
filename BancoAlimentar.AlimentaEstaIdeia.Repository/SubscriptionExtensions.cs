// -----------------------------------------------------------------------
// <copyright file="SubscriptionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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

    /// <summary>
    /// Extension methods for <see cref="Subscription"/>.
    /// </summary>
    public static class SubscriptionExtensions
    {
        /// <summary>
        /// Filter the subscription.
        /// </summary>
        /// <param name="value">The current subscription.</param>
        /// <returns>The same subscription.</returns>
        public static Subscription FilterSubcription(this Subscription value)
        {
            if (value != null)
            {
                if (value.ExpirationTime < DateTime.UtcNow)
                {
                    value.Status = SubscriptionStatus.Expired;
                }
            }

            return value;
        }

        /// <summary>
        /// Filter a collection of subscriptions.
        /// </summary>
        /// <param name="value">A collection of subscriptions.</param>
        /// <returns>The collection of subscriptions.</returns>
        public static List<Subscription> FilterSubcription(this List<Subscription> value)
        {
            if (value != null)
            {
                foreach (var item in value)
                {
                    if (item.ExpirationTime < DateTime.UtcNow)
                    {
                        item.Status = SubscriptionStatus.Expired;
                    }
                }
            }

            return value;
        }
    }
}
