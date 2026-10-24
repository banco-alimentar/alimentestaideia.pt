// -----------------------------------------------------------------------
// <copyright file="AdminReferralsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the admin referrals page.
    /// </summary>
    public class AdminReferralsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "admin-referrals-list@test.com";
        private const string Password = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminReferralsTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminReferralsTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Counts only paid donations in the referral list.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Index_CountsOnlyPaidDonations()
        {
            const string imageUrl = "uploads/referrals/1/campaign.png";
            DateTime lastPaidDonationDate = DateTime.UtcNow.AddDays(-1);
            DateTime pendingDonationDate = lastPaidDonationDate.AddDays(2);
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = this.factory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(
                    scope.ServiceProvider,
                    imageUrl: imageUrl);
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid(),
                    referralId: referralSeed.ReferralId,
                    donationDate: DateTime.UtcNow.AddDays(-2));
                var lastPaidDonation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid(),
                    referralId: referralSeed.ReferralId,
                    donationDate: lastPaidDonationDate);

                var pendingDonation = await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid());
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var referral = await context.Referrals.FindAsync(referralSeed.ReferralId);
                referral.LinkOpenCount = 10;
                lastPaidDonation.DonationAmount = 7;
                pendingDonation.Donation.DonationDate = pendingDonationDate;
                pendingDonation.Donation.ReferralEntity = await context.Referrals.FindAsync(referralSeed.ReferralId);
                await context.SaveChangesAsync();
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password);
            var response = await client.GetAsync(
                "/Admin/Referrals?sortBy=DonationCount&sortDescending=true");
            response.EnsureSuccessStatusCode();
            string html = await response.Content.ReadAsStringAsync();

            int referralLinkIndex = html.IndexOf($">{referralSeed.Code}</a>", StringComparison.Ordinal);
            Assert.True(referralLinkIndex >= 0, "The seeded referral was not rendered.");
            int rowStartIndex = html.LastIndexOf("<tr", referralLinkIndex, StringComparison.Ordinal);
            int rowEndIndex = html.IndexOf("</tr>", referralLinkIndex, StringComparison.Ordinal);
            string row = html.Substring(rowStartIndex, rowEndIndex - rowStartIndex);

            Assert.Matches(@"<td>\s*2\s*</td>", row);
            Assert.Contains("<td>10</td>", row, StringComparison.Ordinal);
            Assert.Contains("<td>5.00</td>", row, StringComparison.Ordinal);
            Assert.Contains("12.00", row, StringComparison.Ordinal);
            Assert.Contains(lastPaidDonationDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), row, StringComparison.Ordinal);
            Assert.DoesNotContain(pendingDonationDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture), row, StringComparison.Ordinal);
            Assert.Contains($"src=\"/{imageUrl}\"", row, StringComparison.Ordinal);

            int imageHeaderIndex = html.IndexOf("Image", StringComparison.Ordinal);
            int codeHeaderIndex = html.IndexOf("Referral Code", StringComparison.Ordinal);
            Assert.True(imageHeaderIndex >= 0, "The image column was not rendered.");
            Assert.True(imageHeaderIndex < codeHeaderIndex, "The image column should be first.");
        }
    }
}
