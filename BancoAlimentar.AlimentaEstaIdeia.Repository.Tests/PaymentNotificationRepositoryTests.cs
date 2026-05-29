// -----------------------------------------------------------------------
// <copyright file="PaymentNotificationRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// Unit tests for <see cref="PaymentNotificationRepository"/>.
    /// </summary>
    public class PaymentNotificationRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly PaymentNotificationRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentNotificationRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public PaymentNotificationRepositoryTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<PaymentNotificationRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Detects whether an email notification already exists for a payment.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanTrackEmailNotificationExistence()
        {
            var payment = await this.SeedMultiBankPaymentAsync();

            Assert.False(this.repository.EmailNotificationExits(payment.Id));

            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            this.repository.AddEmailNotification(user, payment);

            Assert.True(this.repository.EmailNotificationExits(payment.Id));
        }

        /// <summary>
        /// Sets a placeholder address when the user has no address on file.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task AddEmailNotificationSetsPlaceholderAddressWhenMissing()
        {
            var payment = await this.SeedMultiBankPaymentAsync();
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = $"no-address-{Guid.NewGuid():N}@example.com",
                UserName = "No Address User",
                NormalizedEmail = $"NO-ADDRESS-{Guid.NewGuid():N}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(user);
            await this.context.SaveChangesAsync();

            this.repository.AddEmailNotification(user, payment);

            var savedUser = await this.context.WebUser.Include(u => u.Address).FirstAsync(u => u.Id == user.Id);
            Assert.Equal("NO-ADDRESS", savedUser.Address.Address1);
        }

        /// <summary>
        /// Finds multibanco payments in the reminder window without prior email notifications.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact(Skip = "Uses EF.Functions.DateDiffDay, which is not supported by the in-memory provider.")]
        public async Task CanGetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications()
        {
            var payment = await this.SeedMultiBankPaymentAsync(createdDaysAgo: 4);

            var result = this.repository.GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications();

            Assert.Contains(result, p => p.Id == payment.Id);
        }

        private async Task<MultiBankPayment> SeedMultiBankPaymentAsync(int createdDaysAgo = 0)
        {
            var donation = await this.context.Donations
                .Include(d => d.User)
                .FirstAsync(d => d.Id == this.fixture.DonationId);

            var payment = new MultiBankPayment
            {
                Created = DateTime.UtcNow.AddDays(-createdDaysAgo),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPayPaymentId = Guid.NewGuid().ToString(),
                Status = null,
                Donation = donation,
            };
            this.context.Payments.Add(payment);
            await this.context.SaveChangesAsync();
            return payment;
        }
    }
}
