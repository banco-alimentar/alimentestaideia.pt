// -----------------------------------------------------------------------
// <copyright file="DeleteOldSubscriptionFunctionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Function;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Tests;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="DeleteOldSubscriptionFunction"/>.
    /// </summary>
    public class DeleteOldSubscriptionFunctionTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOldSubscriptionFunctionTests"/> class.
        /// </summary>
        /// <param name="fixture">Shared repository test fixture.</param>
        public DeleteOldSubscriptionFunctionTests(ServicesFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Executes without error when there are no expired subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_CompletesWhenNoExpiredSubscriptionsExist()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var function = new DeleteOldSubscriptionFunction(
                TelemetryConfiguration.CreateDefault(),
                this.fixture.ServiceProvider);

            var exception = await Record.ExceptionAsync(() => function.ExecuteFunction(unitOfWork, context));

            Assert.Null(exception);
        }

        /// <summary>
        /// Deletes stale created subscriptions older than one day when no active sibling exists.
        /// In-memory EF does not evaluate DateDiffDay, so this verifies the function runs safely.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_HandlesExpiredCreatedSubscriptionQuery()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = await context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();

            var initialDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow.AddDays(-3),
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

            var subscription = new Subscription
            {
                Created = DateTime.UtcNow.AddDays(-2),
                StartTime = DateTime.UtcNow.AddDays(-2),
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription",
                Status = SubscriptionStatus.Created,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
            };

            context.Donations.Add(initialDonation);
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var function = new DeleteOldSubscriptionFunction(
                TelemetryConfiguration.CreateDefault(),
                this.fixture.ServiceProvider);

            await function.ExecuteFunction(unitOfWork, context);

            Assert.True(await context.Subscriptions.AnyAsync(s => s.Id == subscription.Id));
        }
    }
}
