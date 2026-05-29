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
        /// Does not replace an existing address when recording email notification.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task AddEmailNotificationPreservesExistingAddress()
        {
            var payment = await this.SeedMultiBankPaymentAsync();
            var user = new WebUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = $"has-address-{Guid.NewGuid():N}@example.com",
                UserName = "Has Address User",
                NormalizedEmail = $"HAS-ADDRESS-{Guid.NewGuid():N}@EXAMPLE.COM",
                Address = new DonorAddress
                {
                    Address1 = "Rua Real",
                    City = "Lisboa",
                    Country = "PT",
                },
            };
            this.context.WebUser.Add(user);
            await this.context.SaveChangesAsync();

            this.repository.AddEmailNotification(user, payment);

            var savedUser = await this.context.WebUser.Include(u => u.Address).FirstAsync(u => u.Id == user.Id);
            Assert.Equal("Rua Real", savedUser.Address.Address1);
        }

        /// <summary>
        /// Finds multibanco payments in the reminder window without prior email notifications.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanGetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications()
        {
            var payment = await this.SeedMultiBankPaymentAsync(createdDaysAgo: 4);

            var result = this.repository.GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications();

            Assert.Contains(result, p => p.Id == payment.Id);
        }

        /// <summary>
        /// Excludes multibanco payments outside the 3–6 day reminder window.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetMultiBankPaymentsSinceLast3DaysIgnoresPaymentsOutsideWindow()
        {
            var tooRecent = await this.SeedMultiBankPaymentAsync(createdDaysAgo: 1);
            var tooOld = await this.SeedMultiBankPaymentAsync(createdDaysAgo: 10);

            var result = this.repository.GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications();

            Assert.DoesNotContain(result, p => p.Id == tooRecent.Id);
            Assert.DoesNotContain(result, p => p.Id == tooOld.Id);
        }

        /// <summary>
        /// Excludes multibanco payments that already have an email notification.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task GetMultiBankPaymentsSinceLast3DaysExcludesPaymentsWithEmailNotification()
        {
            var payment = await this.SeedMultiBankPaymentAsync(createdDaysAgo: 4);
            var user = await this.context.WebUser.FirstAsync(u => u.Id == this.fixture.UserId);
            this.repository.AddEmailNotification(user, payment);

            var result = this.repository.GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications();

            Assert.DoesNotContain(result, p => p.Id == payment.Id);
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
