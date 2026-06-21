// -----------------------------------------------------------------------
// <copyright file="DeleteOldSubscriptionFunctionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Tests;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DeleteOldSubscriptionFunction"/>.
    /// </summary>
    public class DeleteOldSubscriptionFunctionTests
    {
        /// <summary>
        /// DeleteDonation removes line items for a waiting-payment donation used by subscription cleanup.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task DeleteDonation_RemovesItemsForWaitingPaymentInitialDonation()
        {
            var fixture = this.CreateFixture();
            var (context, unitOfWork) = this.CreateSharedWorkContext(fixture);
            var user = await context.WebUser.FirstAsync(u => u.Id == fixture.UserId);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var initialDonation = await this.SeedInitialDonationAsync(context, user, foodBank, product);

            var exception = Record.Exception(() => unitOfWork.Donation.DeleteDonation(initialDonation.Id));

            Assert.Null(exception);
            Assert.Null(await context.Donations.FindAsync(initialDonation.Id));
        }

        /// <summary>
        /// Executes without error when there are no expired subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_CompletesWhenNoExpiredSubscriptionsExist()
        {
            var fixture = this.CreateFixture();
            var (context, unitOfWork) = this.CreateSharedWorkContext(fixture);
            var function = this.CreateFunction(fixture);

            var exception = await Record.ExceptionAsync(() => function.ExecuteFunction(unitOfWork, context));

            Assert.Null(exception);
        }

        /// <summary>
        /// Deletes stale created subscriptions older than one day when no active sibling exists.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_DeletesExpiredCreatedSubscription_WhenNoActiveSiblingExists()
        {
            var fixture = this.CreateFixture();
            var (context, unitOfWork) = this.CreateSharedWorkContext(fixture);
            var subscription = await this.SeedCreatedSubscriptionAsync(fixture, context, DateTime.UtcNow.AddDays(-2));
            var initialDonationId = subscription.InitialDonation.Id;
            var function = this.CreateFunction(fixture);

            await function.ExecuteFunction(unitOfWork, context);

            Assert.False(await context.Subscriptions.AnyAsync(s => s.Id == subscription.Id));
            Assert.Null(await context.Donations.FindAsync(initialDonationId));
        }

        /// <summary>
        /// Keeps the initial donation when another active subscription exists for it.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_PreservesInitialDonation_WhenActiveSiblingExists()
        {
            var fixture = this.CreateFixture();
            var (context, unitOfWork) = this.CreateSharedWorkContext(fixture);
            var user = await context.WebUser.FirstAsync(u => u.Id == fixture.UserId);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var initialDonation = await this.SeedInitialDonationAsync(context, user, foodBank, product);

            var expiredSubscription = new Subscription
            {
                Created = DateTime.UtcNow.AddDays(-2),
                StartTime = DateTime.UtcNow.AddDays(-2),
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription-expired",
                Status = SubscriptionStatus.Created,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
                Donations = new List<SubscriptionDonations>(),
            };
            var activeSubscription = new Subscription
            {
                Created = DateTime.UtcNow.AddDays(-10),
                StartTime = DateTime.UtcNow.AddDays(-10),
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription-active",
                Status = SubscriptionStatus.Active,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
                Donations = new List<SubscriptionDonations>(),
            };
            context.Subscriptions.AddRange(expiredSubscription, activeSubscription);
            await context.SaveChangesAsync();

            var function = this.CreateFunction(fixture);
            await function.ExecuteFunction(unitOfWork, context);

            Assert.False(await context.Subscriptions.AnyAsync(s => s.Id == expiredSubscription.Id));
            Assert.True(await context.Subscriptions.AnyAsync(s => s.Id == activeSubscription.Id));
            Assert.True(await context.Donations.AnyAsync(d => d.Id == initialDonation.Id));
        }

        /// <summary>
        /// Does not delete active subscriptions even when they are old.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_DoesNotDeleteActiveSubscriptions()
        {
            var fixture = this.CreateFixture();
            var (context, unitOfWork) = this.CreateSharedWorkContext(fixture);
            var user = await context.WebUser.FirstAsync(u => u.Id == fixture.UserId);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var initialDonation = await this.SeedInitialDonationAsync(context, user, foodBank, product);
            var activeSubscription = new Subscription
            {
                Created = DateTime.UtcNow.AddDays(-5),
                StartTime = DateTime.UtcNow.AddDays(-5),
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription-active-old",
                Status = SubscriptionStatus.Active,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
                Donations = new List<SubscriptionDonations>(),
            };
            context.Subscriptions.Add(activeSubscription);
            await context.SaveChangesAsync();

            var function = this.CreateFunction(fixture);
            await function.ExecuteFunction(unitOfWork, context);

            Assert.True(await context.Subscriptions.AnyAsync(s => s.Id == activeSubscription.Id));
        }

        private ServicesFixture CreateFixture()
        {
            return new ServicesFixture();
        }

        private DeleteOldSubscriptionFunction CreateFunction(ServicesFixture fixture)
        {
            return new DeleteOldSubscriptionFunction(
                TelemetryConfiguration.CreateDefault(),
                fixture.ServiceProvider);
        }

        private (ApplicationDbContext Context, IUnitOfWork UnitOfWork) CreateSharedWorkContext(ServicesFixture fixture)
        {
            var context = fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var telemetry = fixture.ServiceProvider.GetRequiredService<TelemetryClient>();
            var cache = fixture.ServiceProvider.GetRequiredService<IMemoryCache>();
            var nifValidator = fixture.ServiceProvider.GetRequiredService<NifApiValidator>();
            var unitOfWork = new UnitOfWork(context, telemetry, cache, nifValidator);
            return (context, unitOfWork);
        }

        private async Task<Subscription> SeedCreatedSubscriptionAsync(
            ServicesFixture fixture,
            ApplicationDbContext context,
            DateTime created)
        {
            var user = await context.WebUser.FirstAsync(u => u.Id == fixture.UserId);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var initialDonation = await this.SeedInitialDonationAsync(context, user, foodBank, product);
            var subscription = new Subscription
            {
                Created = created,
                StartTime = created,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription",
                Status = SubscriptionStatus.Created,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
                Donations = new List<SubscriptionDonations>(),
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
            return subscription;
        }

        private async Task<Donation> SeedInitialDonationAsync(
            ApplicationDbContext context,
            Model.Identity.WebUser user,
            FoodBank foodBank,
            ProductCatalogue product)
        {
            var initialDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow.AddDays(-3),
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
                DonationItems = new List<DonationItem>
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
            context.Donations.Add(initialDonation);
            await context.SaveChangesAsync();
            return initialDonation;
        }
    }
}
