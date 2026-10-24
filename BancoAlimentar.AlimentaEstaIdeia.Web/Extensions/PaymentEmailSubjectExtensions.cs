// -----------------------------------------------------------------------
// <copyright file="PaymentEmailSubjectExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Adds referral campaign context to payment email subjects.
    /// </summary>
    internal static class PaymentEmailSubjectExtensions
    {
        /// <summary>
        /// Adds the referral campaign name to a payment email subject when applicable.
        /// </summary>
        /// <param name="subject">Base email subject.</param>
        /// <param name="donation">Paid donation.</param>
        /// <returns>The subject with referral campaign context when applicable.</returns>
        public static string AddReferralCampaign(this string subject, Donation donation)
        {
            string campaignName = donation?.ReferralEntity?.Name;
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(campaignName))
            {
                return subject;
            }

            return string.Concat(subject, " no âmbito da campanha ", campaignName);
        }
    }
}
