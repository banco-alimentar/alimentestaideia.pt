// -----------------------------------------------------------------------
// <copyright file="SubscriptionForecastHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;

    /// <summary>
    /// Forecasts upcoming subscription donations for reporting.
    /// </summary>
    public static class SubscriptionForecastHelper
    {
        /// <summary>
        /// Counts donations scheduled between the forecast window and subscription expiration.
        /// </summary>
        /// <param name="forecastStart">Start of the forecast window (typically report generation time).</param>
        /// <param name="forecastEnd">End of the forecast window (typically active campaign report end).</param>
        /// <param name="subscriptionExpiration">Subscription expiration date.</param>
        /// <param name="lastPaidDonationDate">Last paid donation date, if any.</param>
        /// <param name="subscriptionStart">Subscription start date.</param>
        /// <param name="frequency">Stored subscription frequency.</param>
        /// <returns>Number of upcoming donations in the forecast window.</returns>
        public static int CountUpcomingDonations(
            DateTime forecastStart,
            DateTime forecastEnd,
            DateTime subscriptionExpiration,
            DateTime? lastPaidDonationDate,
            DateTime subscriptionStart,
            string frequency)
        {
            if (!SubscriptionFrequencyHelper.TryParseFrequency(frequency, out _))
            {
                return 0;
            }

            DateTime periodEnd = forecastEnd < subscriptionExpiration ? forecastEnd : subscriptionExpiration;
            if (periodEnd <= forecastStart)
            {
                return 0;
            }

            DateTime referenceDate = lastPaidDonationDate ?? subscriptionStart;
            DateTime? nextDonation = SubscriptionFrequencyHelper.AddFrequency(referenceDate, frequency);
            if (!nextDonation.HasValue)
            {
                return 0;
            }

            while (nextDonation.Value < forecastStart)
            {
                nextDonation = SubscriptionFrequencyHelper.AddFrequency(nextDonation.Value, frequency);
                if (!nextDonation.HasValue)
                {
                    return 0;
                }
            }

            int count = 0;
            DateTime cursor = nextDonation.Value;
            while (cursor <= periodEnd)
            {
                count++;
                DateTime? following = SubscriptionFrequencyHelper.AddFrequency(cursor, frequency);
                if (!following.HasValue)
                {
                    break;
                }

                cursor = following.Value;
            }

            return count;
        }
    }
}
