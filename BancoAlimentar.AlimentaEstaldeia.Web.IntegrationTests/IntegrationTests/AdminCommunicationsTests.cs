// -----------------------------------------------------------------------
// <copyright file="AdminCommunicationsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the admin non-payment communications page.
    /// </summary>
    public class AdminCommunicationsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "admin-communications-list@test.com";
        private const string Password = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminCommunicationsTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminCommunicationsTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Lists account communications and excludes records linked to payments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Index_ShowsNonPaymentCommunicationsAndExcludesPaymentNotifications()
        {
            string accountSubject = "Account communication " + Guid.NewGuid().ToString("N");
            string paymentSubject = "Payment communication " + Guid.NewGuid().ToString("N");
            string recipientEmail;
            using (var scope = this.factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var donation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    Guid.NewGuid());
                var user = donation.User;
                var payment = donation.ConfirmedPayment;
                recipientEmail = user.Email;

                context.EmailCommunications.AddRange(
                    new EmailCommunication
                    {
                        FromAddress = "noreply@test.com",
                        ToAddress = recipientEmail,
                        SentAtUtc = DateTime.UtcNow,
                        Subject = accountSubject,
                        UserId = user.Id,
                    },
                    new EmailCommunication
                    {
                        FromAddress = "noreply@test.com",
                        ToAddress = recipientEmail,
                        SentAtUtc = DateTime.UtcNow,
                        Subject = paymentSubject,
                        UserId = user.Id,
                        PaymentId = payment.Id,
                    });
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);
                await context.SaveChangesAsync();
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password);
            var response = await client.GetAsync("/Admin/Communications");
            string html = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            Assert.Contains(accountSubject, html, StringComparison.Ordinal);
            Assert.Contains(recipientEmail, html, StringComparison.Ordinal);
            Assert.DoesNotContain(paymentSubject, html, StringComparison.Ordinal);
        }

        /// <summary>
        /// Filters communications, sorts the filtered results, and paginates them.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Index_FiltersSortsAndPaginatesCommunications()
        {
            string filter = "communications-filter-" + Guid.NewGuid().ToString("N");
            using (var scope = this.factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                for (int index = 0; index < 26; index++)
                {
                    context.EmailCommunications.Add(new EmailCommunication
                    {
                        FromAddress = $"sender-{index}@test.com",
                        ToAddress = $"recipient-{index}@test.com",
                        SentAtUtc = DateTime.UtcNow.AddMinutes(-index),
                        Subject = $"{filter} {index:D2}",
                    });
                }

                await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    Password);
                await context.SaveChangesAsync();
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password);
            var response = await client.GetAsync(
                $"/Admin/Communications?search={Uri.EscapeDataString(filter)}&pageIndex=2&sortBy=Subject&sortDescending=false");
            string html = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            Assert.Contains("Page 2 of 2", html, StringComparison.Ordinal);
            Assert.Contains($"{filter} 25", html, StringComparison.Ordinal);
            Assert.DoesNotContain($"{filter} 00</td>", html, StringComparison.Ordinal);
            Assert.Contains($"search={Uri.EscapeDataString(filter)}", html, StringComparison.Ordinal);
            Assert.Contains("sortBy=Subject", html, StringComparison.Ordinal);
        }
    }
}
