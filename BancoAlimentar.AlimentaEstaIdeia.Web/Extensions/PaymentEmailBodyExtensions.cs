// -----------------------------------------------------------------------
// <copyright file="PaymentEmailBodyExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Provides referral campaign content for payment email bodies.
    /// </summary>
    public static class PaymentEmailBodyExtensions
    {
        /// <summary>
        /// Gets the optional inline referral campaign text for a payment email body.
        /// </summary>
        /// <param name="donation">The donation associated with the email.</param>
        /// <returns>The HTML text, or an empty string when no referral campaign exists.</returns>
        public static string GetReferralCampaignBody(this Donation donation)
        {
            string campaignName = donation?.ReferralEntity?.Name;
            if (string.IsNullOrWhiteSpace(campaignName))
            {
                return string.Empty;
            }

            return string.Concat(
                " no contexto da campanha <b>",
                WebUtility.HtmlEncode(campaignName),
                "</b>");
        }

        /// <summary>
        /// Includes referral context in the thank-you sentence, including in older configured templates.
        /// </summary>
        /// <param name="body">Payment confirmation HTML template.</param>
        /// <param name="donation">The donation associated with the email.</param>
        /// <returns>The email body with the optional campaign context.</returns>
        public static string AddReferralCampaignToBody(this string body, Donation donation)
        {
            // Older templates either lack the placeholder or put it after the completed sentence.
            body = body.Replace("{referralCampaignName}", string.Empty, StringComparison.Ordinal);
            string campaignContext = donation.GetReferralCampaignBody();
            if (campaignContext.Length == 0)
            {
                return body;
            }

            const string thankYou = "Muito obrigado pela sua participação na campanha do Banco Alimentar";
            const string shortThankYou = "Muito obrigado pela sua participação nesta campanha";
            const string subscriptionThankYou = "Muito obrigado pela sua doação regular ao Banco Alimentar";

            foreach (string message in new[] { thankYou, shortThankYou, subscriptionThankYou })
            {
                string original = body.Contains(message + ".", StringComparison.Ordinal) ? message + "." : message;
                string replacement = message == subscriptionThankYou ? subscriptionThankYou : thankYou;
                body = body.Replace(original, string.Concat(replacement, campaignContext), StringComparison.Ordinal);
            }

            return body;
        }
    }
}
