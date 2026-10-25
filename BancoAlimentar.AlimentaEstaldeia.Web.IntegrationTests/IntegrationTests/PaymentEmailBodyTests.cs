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

            Assert.Equal(
                $" no contexto da campanha <b>{WebUtility.HtmlEncode("Coração à Mesa: Alimente esta ideia!")}</b>",
                body);
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

        /// <summary>
        /// Old and current templates put campaign context inside the original thank-you sentence.
        /// </summary>
        /// <param name="template">Current or legacy payment confirmation template.</param>
        [Theory]
        [InlineData("<p>Muito obrigado pela sua participação na campanha do Banco Alimentar.</p><p>Pagamento confirmado.</p>")]
        [InlineData("<p>Muito obrigado pela sua participação na campanha do Banco Alimentar</p><p>Pagamento confirmado.</p>")]
        [InlineData("<p>Muito obrigado pela sua participação na campanha do Banco Alimentar.</p>{referralCampaignName}<p>Pagamento confirmado.</p>")]
        [InlineData("<p>Muito obrigado pela sua participação nesta campanha</p>{referralCampaignName}<p>Pagamento confirmado.</p>")]
        public void AddReferralCampaignToBody_IncludesCampaignInThankYouSentence(string template)
        {
            const string campaignName = "Coração à Mesa: Alimente esta ideia!";
            var donation = new Donation { ReferralEntity = new Referral { Name = campaignName } };

            string body = template.AddReferralCampaignToBody(donation);

            string expectedSentence = $"Muito obrigado pela sua participação na campanha do Banco Alimentar no contexto da campanha <b>{WebUtility.HtmlEncode(campaignName)}</b>";
            Assert.Equal($"<p>{expectedSentence}</p><p>Pagamento confirmado.</p>", body);
            Assert.DoesNotContain("{referralCampaignName}", body);
            Assert.DoesNotContain("<br", body);
            Assert.DoesNotContain("A sua doação foi realizada", body);
        }

        /// <summary>
        /// Campaign names containing markup are rendered as text rather than executable HTML.
        /// </summary>
        [Fact]
        public void AddReferralCampaignToBody_HtmlEncodesCampaignName()
        {
            const string campaignName = "Coração & Mesa <script>alert('campaign')</script>";
            const string template = "<p>Muito obrigado pela sua participação na campanha do Banco Alimentar.</p>";
            var donation = new Donation { ReferralEntity = new Referral { Name = campaignName } };

            string body = template.AddReferralCampaignToBody(donation);

            Assert.Contains(WebUtility.HtmlEncode(campaignName), body);
            Assert.DoesNotContain("<script>", body);
        }

        /// <summary>
        /// Payment confirmations without a named referral preserve their original content.
        /// </summary>
        /// <param name="campaignName">An empty or missing referral campaign name.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddReferralCampaignToBody_PreservesThankYouWithoutNamedCampaign(string campaignName)
        {
            const string thankYou = "<p>Muito obrigado pela sua participação na campanha do Banco Alimentar.</p>";
            var donation = new Donation { ReferralEntity = new Referral { Name = campaignName } };

            Assert.Equal(thankYou, thankYou.AddReferralCampaignToBody(donation));
            Assert.Equal(thankYou, (thankYou + "{referralCampaignName}").AddReferralCampaignToBody(donation));
        }
    }
}
