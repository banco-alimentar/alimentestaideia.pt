// -----------------------------------------------------------------------
// <copyright file="UserRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="UserRepository"/>.
    /// </summary>
    public class UserRepositoryTests : IClassFixture<ServicesFixture>
    {
        private readonly ServicesFixture fixture;
        private readonly UserRepository repository;
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepositoryTests"/> class.
        /// </summary>
        /// <param name="servicesFixture">Shared services fixture.</param>
        public UserRepositoryTests(ServicesFixture servicesFixture)
        {
            this.fixture = servicesFixture;
            this.repository = servicesFixture.ServiceProvider.GetRequiredService<UserRepository>();
            this.context = servicesFixture.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        /// <summary>
        /// Creates a new anonymous user when the email is unknown, then returns the same user on repeat.
        /// </summary>
        [Fact]
        public void CanFindOrCreateWebUser()
        {
            string email = $"repo-test-{Guid.NewGuid():N}@example.com";
            var address = new DonorAddress { Address1 = "Rua Teste", City = "Lisboa", Country = "PT" };

            var created = this.repository.FindOrCreateWebUser(email, "Company", "123456789", "Test User", address);
            var existing = this.repository.FindOrCreateWebUser(email, "Other", "987654321", "Other Name", address);

            Assert.NotNull(created.Id);
            Assert.Equal(created.Id, existing.Id);
            Assert.True(created.IsAnonymous);
            Assert.Equal(email.ToUpperInvariant(), created.NormalizedEmail);
        }

        /// <summary>
        /// Empty user id returns null.
        /// </summary>
        [Fact]
        public void FindUserByIdReturnsNullForEmptyId()
        {
            Assert.Null(this.repository.FindUserById(string.Empty));
        }

        /// <summary>
        /// Finds a user by primary key.
        /// </summary>
        [Fact]
        public void CanFindUserById()
        {
            var result = this.repository.FindUserById(this.fixture.UserId);

            Assert.NotNull(result);
            Assert.Equal(this.fixture.UserId, result.Id);
        }

        /// <summary>
        /// Updates profile fields for an existing anonymous donor.
        /// </summary>
        [Fact]
        public void CanUpdateAnonymousUserData()
        {
            string email = $"anonymous-update-{Guid.NewGuid():N}@example.com";
            var address = new DonorAddress
            {
                Address1 = "Rua A",
                City = "Porto",
                PostalCode = "4000-001",
                Country = "PT",
            };
            this.repository.FindOrCreateWebUser(email, "Old Co", "111111111", "Old Name", address);

            var updatedAddress = new DonorAddress
            {
                Address1 = "Rua B",
                City = "Lisboa",
                PostalCode = "1000-001",
                Country = "PT",
            };
            this.repository.UpdateAnonymousUserData(email, "New Co", "222222222", "New Name", updatedAddress);

            var user = this.context.WebUser.First(u => u.Email == email);
            Assert.Equal("New Co", user.CompanyName);
            Assert.Equal("New Name", user.FullName);
            Assert.Equal("Rua B", user.Address.Address1);
            Assert.Equal("Lisboa", user.Address.City);
        }

        /// <summary>
        /// Returns the seeded anonymous system user.
        /// </summary>
        [Fact]
        public void CanGetAnonymousUser()
        {
            var result = this.repository.GetAnonymousUser();

            Assert.NotNull(result);
            Assert.Equal(default(Guid).ToString(), result.Id);
        }

        /// <summary>
        /// Does not overwrite NIF when the email is not confirmed.
        /// </summary>
        [Fact]
        public void DoesNotUpdateNifWhenEmailNotConfirmed()
        {
            string email = $"nif-locked-{Guid.NewGuid():N}@example.com";
            var address = new DonorAddress { Address1 = "Rua A", City = "Lisboa", Country = "PT" };
            this.repository.FindOrCreateWebUser(email, "Co", "111111111", "User", address);

            this.repository.UpdateAnonymousUserData(email, "Co", "999999999", "User", address);

            var user = this.context.WebUser.First(u => u.Email == email);
            Assert.False(user.EmailConfirmed);
            Assert.Equal("111111111", user.Nif);
        }

        /// <summary>
        /// Updates NIF when the email is confirmed.
        /// </summary>
        [Fact]
        public void UpdatesNifWhenEmailConfirmed()
        {
            string email = $"nif-open-{Guid.NewGuid():N}@example.com";
            var address = new DonorAddress { Address1 = "Rua B", City = "Porto", Country = "PT" };
            this.repository.FindOrCreateWebUser(email, "Co", "111111111", "User", address);
            var user = this.context.WebUser.First(u => u.Email == email);
            user.EmailConfirmed = true;
            this.context.SaveChanges();

            this.repository.UpdateAnonymousUserData(email, "Co", "222222222", "User", address);

            var updated = this.context.WebUser.AsNoTracking().First(u => u.Email == email);
            Assert.Equal("222222222", updated.Nif);
        }

        /// <summary>
        /// Deletes a user and anonymizes retained donation records.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanDeleteUserAndPersonalData()
        {
            string userId = Guid.NewGuid().ToString();
            var address = new DonorAddress
            {
                Address1 = "Rua Privada",
                City = "Lisboa",
                Country = "PT",
            };
            var user = new WebUser
            {
                Id = userId,
                Email = $"delete-{userId}@example.com",
                UserName = $"delete-{userId}@example.com",
                NormalizedEmail = $"DELETE-{userId}@EXAMPLE.COM",
                Address = address,
            };
            var referral = new Referral
            {
                Active = true,
                Code = $"delete-{Guid.NewGuid():N}",
                CreateDate = DateTime.UtcNow,
                Name = "Private campaign",
                User = user,
            };
            var donation = new Donation
            {
                DonationAmount = 10,
                DonationDate = DateTime.UtcNow,
                Nif = "123456789",
                PaymentStatus = PaymentStatus.WaitingPayment,
                PublicId = Guid.NewGuid(),
                ReferralEntity = referral,
                ServiceEntity = "12345",
                ServiceReference = "123456789",
                User = user,
                WantsReceipt = true,
                PaymentList = new List<BasePayment>(),
            };
            var payment = new CreditCardPayment
            {
                Created = DateTime.UtcNow,
                Donation = donation,
                TransactionKey = Guid.NewGuid().ToString(),
                Url = "https://payments.example.com/private-session",
            };
            donation.PaymentList.Add(payment);
            var mbWayPayment = new MBWayPayment
            {
                Alias = "912345678",
                Created = DateTime.UtcNow,
                Donation = donation,
                TransactionKey = Guid.NewGuid().ToString(),
            };
            donation.PaymentList.Add(mbWayPayment);
            var payPalPayment = new PayPalPayment
            {
                Created = DateTime.UtcNow,
                Donation = donation,
                PayerId = "private-payer",
                PayPalPaymentId = "payment-reference",
                Token = "private-token",
                TransactionKey = Guid.NewGuid().ToString(),
            };
            donation.PaymentList.Add(payPalPayment);
            this.context.WebUser.Add(user);
            this.context.Donations.Add(donation);
            this.context.Invoices.Add(new Invoice
            {
                User = user,
                Donation = donation,
                Created = DateTime.UtcNow,
                Number = "INV-TEST",
                Year = DateTime.UtcNow.Year,
                Sequence = 1,
                SequenceScope = Random.Shared.Next(100000, int.MaxValue),
                BlobName = Guid.NewGuid(),
            });
            this.context.Subscriptions.Add(new Subscription
            {
                InitialDonation = donation,
                IsDeleted = false,
                PublicId = Guid.NewGuid(),
                Status = SubscriptionStatus.Created,
                Url = "https://payments.example.com/private-subscription",
                User = user,
            });
            this.context.PaymentNotifications.Add(new PaymentNotifications
            {
                Created = DateTime.UtcNow,
                NotificationType = NotificationType.Email,
                Payment = payment,
                Subject = "Payment",
                User = user,
            });
            this.context.EmailCommunications.Add(new EmailCommunication
            {
                FromAddress = "noreply@example.com",
                Payment = payment,
                SentAtUtc = DateTime.UtcNow,
                Subject = "Payment",
                ToAddress = user.Email,
                User = user,
                UserId = user.Id,
            });
            await this.context.SaveChangesAsync();

            bool result = this.repository.DeleteUserAndDonations(userId);

            Assert.True(result);
            Assert.Null(await this.context.WebUser.AsNoTracking().FirstOrDefaultAsync(value => value.Id == userId));
            Assert.Null(await this.context.DonorAddresses.AsNoTracking().FirstOrDefaultAsync(value => value.Id == address.Id));
            Assert.Empty(this.context.Invoices.Where(invoice => invoice.User.Id == userId));
            Assert.Empty(this.context.PaymentNotifications.Where(notification => notification.User.Id == userId));
            Assert.Empty(this.context.EmailCommunications.Where(communication => communication.UserId == userId));
            Assert.Empty(this.context.Referrals.Where(value => value.User.Id == userId));

            this.context.ChangeTracker.Clear();
            Donation retainedDonation = await this.context.Donations
                .Include(value => value.User)
                .Include(value => value.ReferralEntity)
                .SingleAsync(value => value.Id == donation.Id);
            Assert.Null(retainedDonation.User);
            Assert.Null(retainedDonation.ReferralEntity);
            Assert.Null(retainedDonation.Nif);
            Assert.Null(retainedDonation.WantsReceipt);
            Assert.Null(retainedDonation.ServiceEntity);
            Assert.Null(retainedDonation.ServiceReference);
            CreditCardPayment retainedPayment = await this.context.CreditCardPayments
                .SingleAsync(value => value.Id == payment.Id);
            Assert.Null(retainedPayment.Url);
            MBWayPayment retainedMbWayPayment = await this.context.MBWayPayments
                .SingleAsync(value => value.Id == mbWayPayment.Id);
            Assert.Null(retainedMbWayPayment.Alias);
            PayPalPayment retainedPayPalPayment = await this.context.PayPalPayments
                .SingleAsync(value => value.Id == payPalPayment.Id);
            Assert.Null(retainedPayPalPayment.PayerId);
            Assert.Null(retainedPayPalPayment.Token);
            Assert.Equal("payment-reference", retainedPayPalPayment.PayPalPaymentId);
            Subscription retainedSubscription = await this.context.Subscriptions
                .Include(value => value.User)
                .SingleAsync(value => value.PublicId != Guid.Empty && value.InitialDonation.Id == donation.Id);
            Assert.Null(retainedSubscription.User);
            Assert.Equal(SubscriptionStatus.Inactive, retainedSubscription.Status);
            Assert.Null(retainedSubscription.Url);
        }

        /// <summary>
        /// Active recurring subscriptions must be cancelled before deleting an account.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CannotDeleteUserWithActiveSubscription()
        {
            string userId = Guid.NewGuid().ToString();
            var user = new WebUser
            {
                Id = userId,
                Email = $"active-subscription-{userId}@example.com",
                UserName = $"active-subscription-{userId}@example.com",
            };
            this.context.WebUser.Add(user);
            this.context.Subscriptions.Add(new Subscription
            {
                IsDeleted = false,
                PublicId = Guid.NewGuid(),
                Status = SubscriptionStatus.Active,
                User = user,
            });
            await this.context.SaveChangesAsync();

            bool result = this.repository.DeleteUserAndDonations(userId);

            Assert.False(result);
            Assert.NotNull(await this.context.WebUser.AsNoTracking().FirstOrDefaultAsync(value => value.Id == userId));
        }
    }
}
