// -----------------------------------------------------------------------
// <copyright file="SubscriptionForecastHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Forecasts upcoming subscription donations for reporting.
    /// </summary>
    public static class SubscriptionForecastHelper
    {
        /// <summary>
        /// Subscription statuses that should generate upcoming donation forecasts.
        /// </summary>
        /// <param name="status">Current subscription status.</param>
        /// <returns>True when upcoming donations should be projected.</returns>
        public static bool IsForecastEligibleStatus(SubscriptionStatus status)
        {
            return status == SubscriptionStatus.Active || status == SubscriptionStatus.Capture;
        }

        /// <summary>
        /// Resolves the forecast horizon for an active campaign from the report generation time.
        /// Uses the later of <see cref="Campaign.End"/> and <see cref="Campaign.ReportEnd"/>.
        /// </summary>
        /// <param name="campaign">Campaign that defines the upcoming period.</param>
        /// <param name="forecastStart">Report generation time.</param>
        /// <returns>Forecast end when the campaign still has upcoming time; otherwise null.</returns>
        public static DateTime? ResolveForecastPeriodEnd(Campaign campaign, DateTime forecastStart)
        {
            if (campaign == null)
            {
                return null;
            }

            DateTime horizon = campaign.End;
            if (campaign.ReportEnd > horizon)
            {
                horizon = campaign.ReportEnd;
            }

            return horizon > forecastStart ? horizon : null;
        }

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
