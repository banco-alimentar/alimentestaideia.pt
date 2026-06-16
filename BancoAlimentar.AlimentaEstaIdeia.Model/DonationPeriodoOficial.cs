// -----------------------------------------------------------------------
// <copyright file="DonationPeriodoOficial.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;

    /// <summary>
    /// Determines whether a donation falls within a campaign's official reporting period.
    /// </summary>
    public static class DonationPeriodoOficial
    {
        /// <summary>
        /// Returns whether the donation date is within the campaign <see cref="Campaign.Start"/>
        /// and <see cref="Campaign.ReportEnd"/> (inclusive).
        /// </summary>
        /// <param name="donationDate">Donation date.</param>
        /// <param name="campaign">Campaign linked to the donation.</param>
        /// <returns><c>true</c> when the date is inside the official period; otherwise <c>false</c>.</returns>
        public static bool IsWithinOfficialPeriod(DateTime donationDate, Campaign campaign)
        {
            if (campaign == null)
            {
                return false;
            }

            return IsWithinOfficialPeriod(donationDate, campaign.Start, campaign.ReportEnd);
        }

        /// <summary>
        /// Returns whether the donation date is within the campaign start and report end (inclusive).
        /// </summary>
        /// <param name="donationDate">Donation date.</param>
        /// <param name="campaignStart">Campaign start.</param>
        /// <param name="campaignReportEnd">Campaign report end.</param>
        /// <returns><c>true</c> when the date is inside the official period; otherwise <c>false</c>.</returns>
        public static bool IsWithinOfficialPeriod(
            DateTime donationDate,
            DateTime campaignStart,
            DateTime campaignReportEnd)
        {
            DateTime localDonationDate = donationDate.Kind == DateTimeKind.Utc
                ? donationDate.ToLocalTime()
                : donationDate;

            return localDonationDate >= campaignStart && localDonationDate <= campaignReportEnd;
        }
    }
}
