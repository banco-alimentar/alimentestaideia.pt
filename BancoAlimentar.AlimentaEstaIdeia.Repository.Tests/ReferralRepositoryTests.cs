// -----------------------------------------------------------------------
// <copyright file="ReferralRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="ReferralRepository"/>.
    /// </summary>
    public class ReferralRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly ReferralRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public ReferralRepositoryTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<ReferralRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Referral lookup is case-insensitive.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetByCodeIsCaseInsensitive()
        {
            var referral = await this.SeedReferralAsync("CaseTest", active: true);

            var result = this.repository.GetByCode("casetest");

            Assert.NotNull(result);
            Assert.Equal(referral.Id, result.Id);
        }

        /// <summary>
        /// Active referral lookup ignores inactive campaigns.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetActiveCampaignsByCodeReturnsNullWhenInactive()
        {
            await this.SeedReferralAsync("InactiveCode", active: false);

            Assert.Null(this.repository.GetActiveCampaignsByCode("inactivecode"));
        }

        /// <summary>
        /// Active referral lookup returns active campaigns.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetActiveCampaignsByCode()
        {
            var referral = await this.SeedReferralAsync("ActiveCode", active: true);

            var result = this.repository.GetActiveCampaignsByCode("activecode");

            Assert.NotNull(result);
            Assert.Equal(referral.Id, result.Id);
        }

        /// <summary>
        /// Campaign lookup returns referrals regardless of active flag.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetCampaignsByCodeWhenInactive()
        {
            var referral = await this.SeedReferralAsync("AnyState", active: false);

            var result = this.repository.GetCampaignsByCode("anystate");

            Assert.NotNull(result);
            Assert.Equal(referral.Id, result.Id);
        }

        /// <summary>
        /// Updates referral active state in the database.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanUpdateState()
        {
            var referral = await this.SeedReferralAsync("ToggleMe", active: true);

            this.repository.UpdateState(referral, false);

            var updated = await this.context.Referrals.AsNoTracking().FirstAsync(r => r.Id == referral.Id);
            Assert.False(updated.Active);
        }

        /// <summary>
        /// Returns only paid donations for the referral code.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetPaidDonationsByReferralCode()
        {
            var referral = await this.SeedReferralAsync("PaidOnly", active: true);
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var foodBank = await this.context.FoodBanks.FirstAsync();
            var product = await this.context.ProductCatalogues.FirstAsync();

            this.context.Donations.Add(new Donation
            {
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                ReferralEntity = referral,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                PublicId = Guid.NewGuid(),
                DonationItems = new[]
                {
                    new DonationItem
                    {
                        ProductCatalogue = product,
                        Quantity = 1,
                        Price = product.Cost,
                    },
                },
            });
            this.context.Donations.Add(new Donation
            {
                DonationAmount = 3,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                ReferralEntity = referral,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
                PublicId = Guid.NewGuid(),
            });
            await this.context.SaveChangesAsync();

            var result = this.repository.GetPaidDonationsByReferralCode("paidonly");

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(PaymentStatus.Payed, result[0].PaymentStatus);
        }

        /// <summary>
        /// Returns null when the referral code does not exist.
        /// </summary>
        [Fact]
        public void GetPaidDonationsByReferralCode_ReturnsNullForUnknownCode()
        {
            var result = this.repository.GetPaidDonationsByReferralCode("does-not-exist");

            Assert.Null(result);
        }

        /// <summary>
        /// Returns all referrals owned by the user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetUserReferrals()
        {
            string userId = Guid.NewGuid().ToString();
            var user = await this.SeedUserAsync(userId);
            await this.SeedReferralAsync("UserRefA", active: true, user: user);
            await this.SeedReferralAsync("UserRefB", active: false, user: user);

            var result = this.repository.GetUserReferrals(userId);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userId, r.User.Id));
        }

        /// <summary>
        /// Top list ranks active referrals by paid donation totals within the evaluation window.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetTopList()
        {
            var topReferral = await this.SeedReferralAsync("TopHigh", active: true);
            var secondReferral = await this.SeedReferralAsync("TopLow", active: true);
            var inactiveReferral = await this.SeedReferralAsync("TopInactive", active: false);
            var staleReferral = await this.SeedReferralAsync("TopStale", active: true);

            await this.SeedPaidDonationForReferralAsync(topReferral, amount: 10000);
            await this.SeedPaidDonationForReferralAsync(secondReferral, amount: 9000);
            await this.SeedPaidDonationForReferralAsync(inactiveReferral, amount: 50000);
            await this.SeedPaidDonationForReferralAsync(
                staleReferral,
                amount: 40000,
                donationDate: DateTime.UtcNow.AddDays(-60));

            var result = this.repository.GetTopList(2, daysToEvaluate: 30);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == topReferral.Id);
            Assert.Contains(result, r => r.Id == secondReferral.Id);
        }

        /// <summary>
        /// Loads referral with donations for the owning user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetFullReferral()
        {
            var referral = await this.SeedReferralAsync("FullLoad", active: true);
            await this.SeedPaidDonationForReferralAsync(referral, amount: 4);

            var result = this.repository.GetFullReferral(this.fixture.UserId, referral.Id);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Donations);
            Assert.NotNull(result.Donations.First().DonationItems.First().ProductCatalogue);
        }

        /// <summary>
        /// GetFullReferral returns null when the referral belongs to another user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetFullReferralReturnsNullForWrongUser()
        {
            var referral = await this.SeedReferralAsync("WrongUserRef", active: true);
            string otherUserId = Guid.NewGuid().ToString();

            var result = this.repository.GetFullReferral(otherUserId, referral.Id);

            Assert.Null(result);
        }

        /// <summary>
        /// GetByCode with user id scopes lookup to that owner's campaigns.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetByCodeWithUserIdReturnsOnlyOwnedReferral()
        {
            var owned = await this.SeedReferralAsync("ScopedCode", active: true);
            var otherUserId = Guid.NewGuid().ToString();
            var otherUser = new WebUser
            {
                Id = otherUserId,
                Email = $"other-{otherUserId}@example.com",
                UserName = $"other-{otherUserId}@example.com",
                NormalizedEmail = $"OTHER-{otherUserId}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(otherUser);
            this.context.Referrals.Add(new Referral
            {
                Code = "otheronly",
                Active = true,
                User = otherUser,
                CreateDate = DateTime.UtcNow,
                IsPublic = true,
                Name = "Other",
            });
            await this.context.SaveChangesAsync();

            Assert.Equal(owned.Id, this.repository.GetByCode("ScopedCode", this.fixture.UserId).Id);
            Assert.Null(this.repository.GetByCode("ScopedCode", otherUserId));
            Assert.NotNull(this.repository.GetByCode("OtherOnly", otherUserId));
        }

        private async Task SeedPaidDonationForReferralAsync(
            Referral referral,
            double amount,
            DateTime? donationDate = null)
        {
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var foodBank = await this.context.FoodBanks.FirstAsync();
            var product = await this.context.ProductCatalogues.FirstAsync();
            var donation = new Donation
            {
                DonationAmount = amount,
                DonationDate = donationDate ?? DateTime.UtcNow,
                FoodBank = foodBank,
                ReferralEntity = referral,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
                PublicId = Guid.NewGuid(),
                DonationItems = new[]
                {
                    new DonationItem
                    {
                        ProductCatalogue = product,
                        Quantity = 1,
                        Price = product.Cost,
                    },
                },
            };
            donation.DonationItems.First().Donation = donation;
            this.context.Donations.Add(donation);
            await this.context.SaveChangesAsync();
        }

        private async Task<WebUser> SeedUserAsync(string userId)
        {
            var user = new WebUser
            {
                Id = userId,
                Email = $"referral-{userId}@example.com",
                UserName = $"referral-{userId}@example.com",
                NormalizedEmail = $"REFERRAL-{userId}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(user);
            await this.context.SaveChangesAsync();
            return user;
        }

        private async Task<Referral> SeedReferralAsync(string code, bool active, WebUser user = null)
        {
            user ??= await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var referral = new Referral
            {
                Code = code.ToLowerInvariant(),
                Active = active,
                User = user,
                CreateDate = DateTime.UtcNow,
                IsPublic = true,
                Name = code,
            };
            this.context.Referrals.Add(referral);
            await this.context.SaveChangesAsync();
            return referral;
        }
    }
}
