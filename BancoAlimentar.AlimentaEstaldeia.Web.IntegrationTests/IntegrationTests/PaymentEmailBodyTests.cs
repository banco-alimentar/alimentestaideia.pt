// -----------------------------------------------------------------------
// <copyright file="PaymentEmailBodyTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Xunit;

    /// <summary>
    /// Tests referral campaign content in payment email bodies.
    /// </summary>
    public class PaymentEmailBodyTests
    {
        /// <summary>
        /// Referral campaign body content includes the campaign name.
        /// </summary>
        [Fact]
        public void GetReferralCampaignBody_IncludesCampaignName()
        {
            var donation = new Donation
            {
                ReferralEntity = new Referral { Name = "Coração à Mesa: Alimente esta ideia!" },
            };

            string body = donation.GetReferralCampaignBody();

            Assert.Contains("no âmbito da campanha", body);
            Assert.Contains(WebUtility.HtmlEncode("Coração à Mesa: Alimente esta ideia!"), body);
        }

        /// <summary>
        /// Referral campaign body content is empty when no campaign exists.
        /// </summary>
        [Fact]
        public void GetReferralCampaignBody_IsEmptyWithoutCampaign()
        {
            var donation = new Donation();

            Assert.Empty(donation.GetReferralCampaignBody());
        }
    }
}
