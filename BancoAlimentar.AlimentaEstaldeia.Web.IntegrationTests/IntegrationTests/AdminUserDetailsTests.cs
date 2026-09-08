// -----------------------------------------------------------------------
// <copyright file="AdminUserDetailsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the admin user details page.
    /// </summary>
    public class AdminUserDetailsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "integration-user-details-admin@test.com";
        private const string DonorEmail = "integration-user-details-donor@test.com";
        private const string Password = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminUserDetailsTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminUserDetailsTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Checks that user donations show whether they are standalone or subscription donations.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Get_UserDetails_ShowsDonationSubscriptionRelationship()
        {
            // Arrange
            IntegrationTestDataSeeder.ActiveSubscriptionSeed subscriptionSeed;
            using (var scope = this.factory.Services.CreateScope())
            {
                subscriptionSeed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    DonorEmail,
                    Password);

                var standaloneDonation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid());
                standaloneDonation.User = await scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>()
                    .Users
                    .SingleAsync(user => user.Id == subscriptionSeed.UserId);
                await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();

                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.EmailCommunications.Add(new EmailCommunication
                {
                    FromAddress = "noreply@integration.test",
                    ToAddress = DonorEmail,
                    SentAtUtc = DateTime.UtcNow,
                    Subject = "Registration confirmation",
                    UserId = subscriptionSeed.UserId,
                });
                await context.SaveChangesAsync();
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password);

            // Act
            var response = await client.GetAsync($"/Admin/Users/Details?id={subscriptionSeed.UserId}");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("One-off donation (not part of a subscription)", html);
            Assert.Contains("Initial subscription donation", html);
            Assert.Contains($"/Admin/Subscriptions/Details?id={subscriptionSeed.SubscriptionId}", html);
            Assert.Contains("Registration confirmation", html);
            Assert.Contains("noreply@integration.test", html);
        }
    }
}
