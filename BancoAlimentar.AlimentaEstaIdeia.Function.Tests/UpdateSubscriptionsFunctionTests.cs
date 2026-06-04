// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptionsFunctionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
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
    /// Tests for <see cref="UpdateSubscriptions"/>.
    /// </summary>
    public class UpdateSubscriptionsFunctionTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptionsFunctionTests"/> class.
        /// </summary>
        /// <param name="fixture">Shared repository test fixture.</param>
        public UpdateSubscriptionsFunctionTests(ServicesFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>
        /// Executes the daily subscription maintenance function without error.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_CompletesSuccessfully()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var function = new UpdateSubscriptions(
                TelemetryConfiguration.CreateDefault(),
                this.fixture.ServiceProvider);

            await function.ExecuteFunction(unitOfWork, context);
        }

        /// <summary>
        /// Executes without error when active subscriptions exist in the database.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_CompletesWhenActiveSubscriptionsExist()
        {
            var fixture = new ServicesFixture();
            var context = fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = await context.WebUser.FirstAsync(u => u.Id == fixture.UserId);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var donation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            donation.DonationItems.First().Donation = donation;
            context.Donations.Add(donation);
            context.Subscriptions.Add(new Subscription
            {
                Created = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = "https://example.com/subscription",
                InitialDonation = donation,
                Frequency = "1M",
                PublicId = Guid.NewGuid(),
                User = user,
                Status = SubscriptionStatus.Active,
            });
            await context.SaveChangesAsync();

            var function = new UpdateSubscriptions(
                TelemetryConfiguration.CreateDefault(),
                fixture.ServiceProvider);

            var exception = await Record.ExceptionAsync(() => function.ExecuteFunction(unitOfWork, context));

            Assert.Null(exception);
        }

        /// <summary>
        /// Repeated execution remains idempotent for the current no-op implementation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task ExecuteFunction_IsIdempotentAcrossRepeatedRuns()
        {
            var context = this.fixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unitOfWork = this.fixture.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var function = new UpdateSubscriptions(
                TelemetryConfiguration.CreateDefault(),
                this.fixture.ServiceProvider);

            await function.ExecuteFunction(unitOfWork, context);
            var exception = await Record.ExceptionAsync(() => function.ExecuteFunction(unitOfWork, context));

            Assert.Null(exception);
        }
    }
}
