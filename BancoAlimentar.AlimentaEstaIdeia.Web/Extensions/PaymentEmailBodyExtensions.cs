// -----------------------------------------------------------------------
// <copyright file="PaymentEmailBodyExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Provides referral campaign content for payment email bodies.
    /// </summary>
    public static class PaymentEmailBodyExtensions
    {
        /// <summary>
        /// Gets the optional referral campaign paragraph for a payment email body.
        /// </summary>
        /// <param name="donation">The donation associated with the email.</param>
        /// <returns>The HTML paragraph, or an empty string when no referral campaign exists.</returns>
        public static string GetReferralCampaignBody(this Donation donation)
        {
            string campaignName = donation?.ReferralEntity?.Name;
            if (string.IsNullOrWhiteSpace(campaignName))
            {
                return string.Empty;
            }

            return string.Concat(
                "<br /><font face=\"calibri\" size=\"3\">A sua doação foi realizada no âmbito da campanha <b>",
                WebUtility.HtmlEncode(campaignName),
                "</b></font>");
        }
    }
}
