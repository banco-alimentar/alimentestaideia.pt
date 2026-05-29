// -----------------------------------------------------------------------
// <copyright file="UserRepositoryTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
        /// Removes invoices linked to the user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task CanDeleteUserInvoices()
        {
            string userId = Guid.NewGuid().ToString();
            var user = new WebUser
            {
                Id = userId,
                Email = $"delete-{userId}@example.com",
                UserName = $"delete-{userId}@example.com",
                NormalizedEmail = $"DELETE-{userId}@EXAMPLE.COM",
            };
            this.context.WebUser.Add(user);
            var donation = await this.context.Donations.FirstAsync();
            this.context.Invoices.Add(new Invoice
            {
                User = user,
                Donation = donation,
                Created = DateTime.UtcNow,
                Number = "INV-TEST",
                Year = DateTime.UtcNow.Year,
                Sequence = 1,
                BlobName = Guid.NewGuid(),
            });
            await this.context.SaveChangesAsync();

            this.repository.DeleteUserAndDonations(userId);

            Assert.Empty(this.context.Invoices.Where(i => i.User.Id == userId));
            Assert.NotNull(await this.context.WebUser.FirstOrDefaultAsync(u => u.Id == userId));
        }
    }
}
