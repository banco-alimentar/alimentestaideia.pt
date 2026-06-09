// -----------------------------------------------------------------------
// <copyright file="SubscriptionFrequencyHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// Parses subscription billing frequency and computes schedule dates.
    /// </summary>
    public static class SubscriptionFrequencyHelper
    {
        /// <summary>
        /// Tries to parse a stored subscription frequency value.
        /// </summary>
        /// <param name="frequency">Stored frequency (for example <c>_1M</c> or <c>1M</c>).</param>
        /// <param name="parsed">Parsed Easypay frequency when successful.</param>
        /// <returns>True when the frequency could be parsed.</returns>
        public static bool TryParseFrequency(string frequency, out SubscriptionPostRequest.FrequencyEnum parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(frequency))
            {
                return false;
            }

            string normalized = frequency.Trim().TrimStart('_');
            return Enum.TryParse("_" + normalized, out parsed);
        }

        /// <summary>
        /// Adds one billing interval to a reference date.
        /// </summary>
        /// <param name="from">Reference date.</param>
        /// <param name="frequency">Stored subscription frequency.</param>
        /// <returns>The next scheduled date, or null when frequency is invalid.</returns>
        public static DateTime? AddFrequency(DateTime from, string frequency)
        {
            if (!TryParseFrequency(frequency, out SubscriptionPostRequest.FrequencyEnum parsed))
            {
                return null;
            }

            return AddFrequency(from, parsed);
        }

        /// <summary>
        /// Adds one billing interval to a reference date.
        /// </summary>
        /// <param name="from">Reference date.</param>
        /// <param name="frequency">Easypay subscription frequency.</param>
        /// <returns>The next scheduled date.</returns>
        public static DateTime AddFrequency(DateTime from, SubscriptionPostRequest.FrequencyEnum frequency)
        {
            string value = frequency.ToString().TrimStart('_');
            string modifier = value.Substring(value.Length - 1, 1);
            int count = int.Parse(value.Substring(0, value.Length - 1), CultureInfo.InvariantCulture);

            return modifier switch
            {
                "D" => from.AddDays(count),
                "W" => from.AddDays(7 * count),
                "M" => from.AddMonths(count),
                "Y" => from.AddYears(count),
                _ => from,
            };
        }
    }
}
