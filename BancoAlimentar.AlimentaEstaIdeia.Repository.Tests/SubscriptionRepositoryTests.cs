// -----------------------------------------------------------------------
// <copyright file="SubscriptionRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Xunit;
    using static Easypay.Rest.Client.Model.SubscriptionPostRequest;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    /// <summary>
    /// Unit tests for <see cref="SubscriptionRepository"/>.
    /// </summary>
    public class SubscriptionRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly SubscriptionRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public SubscriptionRepositoryTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<SubscriptionRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Marks subscription active when easypay reports success.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCompleteSubscriptionCreateOnSuccess()
        {
            var (subscription, _) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Created);
            int subscriptionId = this.repository.CompleteSubcriptionCreate(
                subscription.TransactionKey,
                NotificationGeneric.StatusEnum.Success);

            Assert.Equal(subscription.Id, subscriptionId);
            var updated = await this.context.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
            Assert.Equal(SubscriptionStatus.Active, updated.Status);
        }

        /// <summary>
        /// Marks subscription in error when easypay reports failure.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCompleteSubscriptionCreateOnFailure()
        {
            var (subscription, _) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Created);
            int subscriptionId = this.repository.CompleteSubcriptionCreate(
                subscription.TransactionKey,
                NotificationGeneric.StatusEnum.Failed);

            Assert.Equal(subscription.Id, subscriptionId);
            var updated = await this.context.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
            Assert.Equal(SubscriptionStatus.Error, updated.Status);
        }

        /// <summary>
        /// Unknown transaction keys return -1.
        /// </summary>
        [Fact]
        public void CanNotCompleteSubscriptionCreateForUnknownTransactionKey()
        {
            int subscriptionId = this.repository.CompleteSubcriptionCreate(
                "unknown-transaction-key",
                NotificationGeneric.StatusEnum.Success);

            Assert.Equal(-1, subscriptionId);
        }

        /// <summary>
        /// Creates a subscription linked to the initial donation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCreateSubscription()
        {
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var donation = await this.SeedDonationAsync(DateTime.UtcNow);
            string transactionKey = Guid.NewGuid().ToString();
            string easyPayDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var request = new SubscriptionPostRequest(
                expirationTime: DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                frequency: FrequencyEnum._1M,
                startTime: easyPayDateTime);

            this.repository.CreateSubscription(
                donation,
                transactionKey,
                Guid.NewGuid().ToString(),
                "https://example.com/subscription",
                user,
                request,
                FrequencyEnum._1M);

            var subscription = await this.context.Subscriptions.FirstAsync(s => s.TransactionKey == transactionKey);
            Assert.Equal(donation.Id, subscription.InitialDonation.Id);
            Assert.Equal(user.Id, subscription.User.Id);
            Assert.True(await this.context.SubscriptionDonations.AnyAsync(sd => sd.Subscription.Id == subscription.Id));
        }

        /// <summary>
        /// Skips subscription creation when required fields are missing.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CreateSubscription_DoesNothingWhenUrlOrTransactionKeyMissing()
        {
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var donation = await this.SeedDonationAsync(DateTime.UtcNow);
            var request = new SubscriptionPostRequest(
                expirationTime: DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                frequency: FrequencyEnum._1M,
                startTime: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            int countBefore = await this.context.Subscriptions.CountAsync();

            this.repository.CreateSubscription(
                donation,
                string.Empty,
                Guid.NewGuid().ToString(),
                "https://example.com/subscription",
                user,
                request,
                FrequencyEnum._1M);

            this.repository.CreateSubscription(
                donation,
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                string.Empty,
                user,
                request,
                FrequencyEnum._1M);

            Assert.Equal(countBefore, await this.context.Subscriptions.CountAsync());
        }

        /// <summary>
        /// Returns null when the user argument is null.
        /// </summary>
        [Fact]
        public void GetUserSubscription_ReturnsNullForNullUser()
        {
            Assert.Null(this.repository.GetUserSubscription(null));
        }

        /// <summary>
        /// Returns subscriptions for a user excluding created-only rows.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetUserSubscription()
        {
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);
            await this.SeedSubscriptionAsync(status: SubscriptionStatus.Created);

            var result = this.repository.GetUserSubscription(user);

            Assert.NotNull(result);
            Assert.All(result, s => Assert.NotEqual(SubscriptionStatus.Created, s.Status));
        }

        /// <summary>
        /// Resolves subscription from initial donation id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetSubscriptionFromDonationId()
        {
            var (subscription, donation) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);

            var result = this.repository.GetSubscriptionFromDonationId(donation.Id);

            Assert.NotNull(result);
            Assert.Equal(subscription.Id, result.Id);
        }

        /// <summary>
        /// Resolves subscriptions for multiple donation ids in one query.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetSubscriptionsByDonationIds()
        {
            var (subscription, donation) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);

            var result = this.repository.GetSubscriptionsByDonationIds(new[] { donation.Id, 999999 });

            Assert.Single(result);
            Assert.True(result.TryGetValue(donation.Id, out Subscription mapped));
            Assert.Equal(subscription.Id, mapped.Id);
        }

        /// <summary>
        /// Soft-deletes a subscription and marks it inactive.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanDeleteSubscription()
        {
            var (subscription, _) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);

            bool deleted = this.repository.DeleteSubscription(subscription.Id);

            Assert.True(deleted);
            var updated = await this.context.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
            Assert.True(updated.IsDeleted);
            Assert.Equal(SubscriptionStatus.Inactive, updated.Status);
        }

        /// <summary>
        /// Delete returns false for unknown subscription ids.
        /// </summary>
        [Fact]
        public void CanNotDeleteUnknownSubscription()
        {
            Assert.False(this.repository.DeleteSubscription(999999));
        }

        /// <summary>
        /// Finds subscription by public id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetSubscriptionByPublicId()
        {
            var (subscription, _) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);

            var result = this.repository.GetSubscriptionByPublicId(subscription.PublicId);

            Assert.NotNull(result);
            Assert.Equal(subscription.Id, result.Id);
        }

        /// <summary>
        /// Finds subscription by primary key with user and initial donation loaded.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetSubscriptionById()
        {
            var (subscription, donation) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);

            var result = this.repository.GetSubscriptionById(subscription.Id);

            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotNull(result.InitialDonation);
            Assert.Equal(donation.Id, result.InitialDonation.Id);
        }

        /// <summary>
        /// Finds subscription by easypay subscription id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetSubscriptionByEasyPayId()
        {
            var easyPayId = Guid.NewGuid();
            var (subscription, _) = await this.SeedSubscriptionAsync(
                status: SubscriptionStatus.Active,
                easyPaySubscriptionId: easyPayId.ToString());

            var result = this.repository.GetSubscriptionByEasyPayId(easyPayId);

            Assert.NotNull(result);
            Assert.Equal(subscription.Id, result.Id);
        }

        /// <summary>
        /// Lists donations linked to a subscription.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetDonationsForSubscription()
        {
            var (subscription, donation) = await this.SeedSubscriptionAsync(status: SubscriptionStatus.Active);

            var result = this.repository.GetDonationsForSubscription(subscription.Id);

            Assert.Contains(result, d => d.Id == donation.Id);
        }

        /// <summary>
        /// Finds a donation for a subscription transaction key on a given date.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetDonationFromSubscriptionTransactionKey()
        {
            var donationDate = DateTime.UtcNow.Date;
            var (subscription, donation) = await this.SeedSubscriptionAsync(
                status: SubscriptionStatus.Active,
                initialDonationDate: donationDate);

            var result = this.repository.GetDonationFromSubscriptionTransactionKey(
                subscription.TransactionKey,
                donationDate);

            Assert.NotNull(result);
            Assert.Equal(donation.Id, result.Id);
        }

        /// <summary>
        /// Creates a new donation and payment when capture date differs from the initial donation date.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanCreateSubscriptionDonationAndPayment()
        {
            string transactionKey = Guid.NewGuid().ToString();
            var (_, initialDonation) = await this.SeedSubscriptionAsync(
                status: SubscriptionStatus.Active,
                transactionKey: transactionKey,
                initialDonationDate: DateTime.UtcNow.AddDays(-2));

            int donationId = this.repository.CreateSubscriptionDonationAndPayment(
                Guid.NewGuid().ToString(),
                transactionKey,
                NotificationGeneric.StatusEnum.Success,
                DateTime.UtcNow);

            Assert.True(donationId > 0);
            Assert.NotEqual(initialDonation.Id, donationId);
            Assert.True(await this.context.Payments.AnyAsync(p => p.TransactionKey == transactionKey));
        }

        /// <summary>
        /// Returns an existing capture payment donation id when dates differ.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanSubscriptionCaptureExistingPayment()
        {
            string transactionKey = Guid.NewGuid().ToString();
            var captureDate = DateTime.UtcNow;
            var (_, initialDonation) = await this.SeedSubscriptionAsync(
                status: SubscriptionStatus.Active,
                transactionKey: transactionKey,
                initialDonationDate: captureDate.AddDays(-3));

            var captureDonation = await this.SeedDonationAsync(captureDate);
            var payment = new CreditCardPayment
            {
                Created = captureDate,
                TransactionKey = transactionKey,
                Url = "https://example.com",
                Status = "pending",
                Donation = captureDonation,
            };
            this.context.Payments.Add(payment);
            await this.context.SaveChangesAsync();

            (int donationId, string reason) = this.repository.SubscriptionCapture(
                Guid.NewGuid().ToString(),
                transactionKey,
                NotificationGeneric.StatusEnum.Success,
                captureDate);

            Assert.Equal(captureDonation.Id, donationId);
            Assert.Equal("None", reason);
            Assert.NotEqual(initialDonation.Id, donationId);
        }

        /// <summary>
        /// Marks subscription inactive when Easypay reports an expired subscription.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanSyncSubscriptionFromEasyPayWhenExpired()
        {
            var easyPayId = Guid.NewGuid();
            var (subscription, _) = await this.SeedSubscriptionAsync(
                SubscriptionStatus.Active,
                easyPaySubscriptionId: easyPayId.ToString());
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            string easyPayDateTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var apiMock = new Mock<ISubscriptionPaymentApi>();
            apiMock
                .Setup(a => a.SubscriptionIdGetAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionIdGet200Response(
                    expirationTime: easyPayDateTime,
                    startTime: easyPayDateTime,
                    createdAt: easyPayDateTime));

            await this.repository.SyncSubscriptionFromEasyPay(apiMock.Object, user);

            var updated = await this.context.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
            Assert.Equal(SubscriptionStatus.Inactive, updated.Status);
        }

        /// <summary>
        /// Updates subscription dates from Easypay when still active.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanSyncSubscriptionFromEasyPayWhenActive()
        {
            var easyPayId = Guid.NewGuid();
            var (subscription, _) = await this.SeedSubscriptionAsync(
                SubscriptionStatus.Active,
                easyPaySubscriptionId: easyPayId.ToString());
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            string startTime = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            string expirationTime = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            string createdAt = DateTime.UtcNow.AddMonths(-2).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            var apiMock = new Mock<ISubscriptionPaymentApi>();
            apiMock
                .Setup(a => a.SubscriptionIdGetAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionIdGet200Response(
                    expirationTime: expirationTime,
                    startTime: startTime,
                    createdAt: createdAt));

            await this.repository.SyncSubscriptionFromEasyPay(apiMock.Object, user);

            var updated = await this.context.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
            Assert.Equal(SubscriptionStatus.Active, updated.Status);
            Assert.Equal(expirationTime.FromEasyPayDateTimeString(), updated.ExpirationTime);
            Assert.Equal(startTime.FromEasyPayDateTimeString(), updated.StartTime);
            Assert.Equal(createdAt.FromEasyPayDateTimeString(), updated.Created);
        }

        /// <summary>
        /// Skips Easypay API calls when the user only has created subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task SyncSubscriptionFromEasyPay_DoesNotCallApiWhenUserHasOnlyCreatedSubscriptions()
        {
            string userId = Guid.NewGuid().ToString();
            var user = new WebUser
            {
                Id = userId,
                Email = $"created-only-{userId}@example.com",
                UserName = $"created-only-{userId}@example.com",
                NormalizedEmail = $"CREATED-ONLY-{userId}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(user);
            await this.context.SaveChangesAsync();

            var foodBank = await this.context.FoodBanks.FirstAsync();
            var product = await this.context.ProductCatalogues.FirstAsync();
            var initialDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
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
            initialDonation.DonationItems.First().Donation = initialDonation;
            this.context.Donations.Add(initialDonation);
            this.context.Subscriptions.Add(new Subscription
            {
                Created = DateTime.UtcNow.AddDays(-2),
                StartTime = DateTime.UtcNow.AddDays(-2),
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription-created-only",
                Status = SubscriptionStatus.Created,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
                Donations = new List<SubscriptionDonations>(),
            });
            await this.context.SaveChangesAsync();

            Assert.Empty(this.repository.GetUserSubscription(user));

            var apiMock = new Mock<ISubscriptionPaymentApi>();
            await this.repository.SyncSubscriptionFromEasyPay(apiMock.Object, user);

            apiMock.Verify(
                a => a.SubscriptionIdGetAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Leaves subscription unchanged when Easypay returns no subscription payload.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task SyncSubscriptionFromEasyPay_SkipsSubscriptionWhenApiReturnsNull()
        {
            string userId = Guid.NewGuid().ToString();
            var user = new WebUser
            {
                Id = userId,
                Email = $"null-api-{userId}@example.com",
                UserName = $"null-api-{userId}@example.com",
                NormalizedEmail = $"NULL-API-{userId}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(user);
            await this.context.SaveChangesAsync();

            var easyPayId = Guid.NewGuid();
            var (subscription, _) = await this.SeedSubscriptionAsync(
                SubscriptionStatus.Active,
                easyPaySubscriptionId: easyPayId.ToString(),
                userId: userId);
            var originalExpiration = subscription.ExpirationTime;
            var originalStatus = subscription.Status;
            var apiMock = new Mock<ISubscriptionPaymentApi>();
            apiMock
                .Setup(a => a.SubscriptionIdGetAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SubscriptionIdGet200Response)null);

            await this.repository.SyncSubscriptionFromEasyPay(apiMock.Object, user);

            var updated = await this.context.Subscriptions.AsNoTracking().FirstAsync(s => s.Id == subscription.Id);
            Assert.Equal(originalStatus, updated.Status);
            Assert.Equal(originalExpiration, updated.ExpirationTime);
        }

        /// <summary>
        /// Returns payment-date-equal reason when capture date matches the initial donation date.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task SubscriptionCaptureReturnsPaymentDateEqualWhenSameDayAsInitialDonation()
        {
            string transactionKey = Guid.NewGuid().ToString();
            var captureDate = DateTime.UtcNow;
            await this.SeedSubscriptionAsync(
                SubscriptionStatus.Active,
                transactionKey: transactionKey,
                initialDonationDate: captureDate);

            (int donationId, string reason) = this.repository.SubscriptionCapture(
                Guid.NewGuid().ToString(),
                transactionKey,
                NotificationGeneric.StatusEnum.Success,
                captureDate);

            Assert.Equal(-1, donationId);
            Assert.Contains("PaymentDate is equal", reason);
        }

        /// <summary>
        /// Returns not found when subscription transaction key is unknown.
        /// </summary>
        [Fact]
        public void SubscriptionCaptureReturnsNotFoundForUnknownKey()
        {
            (int donationId, string reason) = this.repository.SubscriptionCapture(
                Guid.NewGuid().ToString(),
                "missing-key",
                NotificationGeneric.StatusEnum.Success,
                DateTime.UtcNow);

            Assert.Equal(-1, donationId);
            Assert.Equal("Subscription is not found", reason);
        }

        private async Task<Donation> SeedDonationAsync(DateTime donationDate)
        {
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var foodBank = await this.context.FoodBanks.FirstAsync();
            var product = await this.context.ProductCatalogues.FirstAsync();
            var donation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = donationDate,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            return donation;
        }

        private async Task<(Subscription Subscription, Donation InitialDonation)> SeedSubscriptionAsync(
            SubscriptionStatus status,
            string transactionKey = null,
            DateTime? initialDonationDate = null,
            string easyPaySubscriptionId = null,
            string userId = null)
        {
            string resolvedUserId = userId ?? this.fixture.UserId;
            var user = await this.context.WebUser.FirstAsync(u => u.Id == resolvedUserId);
            var donation = await this.SeedDonationAsync(initialDonationDate ?? DateTime.UtcNow);
            var subscription = new Subscription
            {
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = transactionKey ?? Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = easyPaySubscriptionId ?? Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription",
                Status = status,
                PublicId = Guid.NewGuid(),
                Frequency = FrequencyEnum._1M.ToString(),
                InitialDonation = donation,
                User = user,
            };
            this.context.Subscriptions.Add(subscription);
            this.context.SubscriptionDonations.Add(new SubscriptionDonations
            {
                Donation = donation,
                Subscription = subscription,
            });
            await this.context.SaveChangesAsync();
            return (subscription, donation);
        }
    }
}
