// -----------------------------------------------------------------------
// <copyright file="SubscriptionManageTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for subscription management pages.
    /// </summary>
    public class SubscriptionManageTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string UserEmail = "integration-user@test.com";
        private const string UserPassword = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionManageTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public SubscriptionManageTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
            using var scope = factory.Services.CreateScope();
            IntegrationTestDataSeeder.EnsureUserAsync(scope.ServiceProvider, UserEmail, UserPassword).Wait();
        }

        /// <summary>
        /// Anonymous users are redirected to login.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RedirectsToLogin_WhenNotAuthenticated()
        {
            var client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/Identity/Account/Manage/Subscriptions");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
        }

        /// <summary>
        /// Authenticated users can open the subscriptions page when the feature is enabled.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ReturnsSuccess_WhenAuthenticated()
        {
            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                UserEmail,
                UserPassword);

            var response = await client.GetAsync("/Identity/Account/Manage/Subscriptions");

            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("id=\"subscriptions\"", html);
            Assert.DoesNotContain("jquery.dataTables.css", html);
            Assert.DoesNotContain("jquery.dataTables.min.css", html);
        }

        /// <summary>
        /// The subscription delete page uses Portuguese translations when the user selects Portuguese.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RendersDeletePageInPortuguese_WhenPortugueseCultureIsSelected()
        {
            IntegrationTestDataSeeder.ActiveSubscriptionSeed seed;
            using (var scope = this.factory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    UserEmail,
                    UserPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                UserEmail,
                UserPassword);
            client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c=pt|uic=pt");

            var response = await client.GetAsync($"/Identity/Account/Manage/Subscriptions/Delete?id={seed.SubscriptionId}");

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Cancelar subscrição", html);
            Assert.Contains("Tem a certeza de que pretende cancelar esta subscrição?", html);
            Assert.Contains("Voltar à lista", html);
            Assert.DoesNotContain("Are you sure you want to cancel this subscription?", html);
        }

        /// <summary>
        /// Authenticated owner can cancel an active subscription via the delete confirmation page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_DeletesSubscription_WhenOwnerConfirmsDelete()
        {
            IntegrationTestDataSeeder.ActiveSubscriptionSeed seed;
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSubscriptionCheckout(services);
                });
            });

            using (var scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    UserEmail,
                    UserPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                UserEmail,
                UserPassword);

            var getResponse = await client.GetAsync($"/Identity/Account/Manage/Subscriptions/Delete?id={seed.SubscriptionId}");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var deleteForm = content.QuerySelector("form[method='post'] input.btn-danger")?.Closest("form");
            Assert.NotNull(deleteForm);
            var postResponse = await client.SendAsync(
                (IHtmlFormElement)deleteForm,
                new Dictionary<string, string>
                {
                    ["Subscription.Id"] = seed.SubscriptionId.ToString(),
                });

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("id=\"subscriptions\"", html);
            Assert.Contains("The subscription was cancelled successfully.", html);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscription = await context.Subscriptions.AsNoTracking()
                .FirstAsync(s => s.Id == seed.SubscriptionId);
            Assert.True(subscription.IsDeleted);
            Assert.Equal(SubscriptionStatus.Inactive, subscription.Status);
        }

        /// <summary>
        /// The subscription details endpoint reports when a local donation status differs from Easypay.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetDataTableData_ReportsEasypayStatusMismatch()
        {
            string donorEmail = $"integration-status-{Guid.NewGuid():N}@test.com";
            string easyPaySubscriptionId = Guid.NewGuid().ToString();
            string easyPayPaymentId = Guid.NewGuid().ToString();
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Easypay:BaseUrl"] = "https://api.integration.test",
                        ["Easypay:AccountId"] = "integration-account",
                        ["Easypay:ApiKey"] = "integration-key",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    IntegrationTestEasyPayConfiguration.AddStubSubscriptionDetails(
                        services,
                        easyPaySubscriptionId,
                        easyPayPaymentId);
                });
            });

            int subscriptionId;
            using (var scope = webFactory.Services.CreateScope())
            {
                var seed = await IntegrationTestDataSeeder.SeedActiveSubscriptionForUserAsync(
                    scope.ServiceProvider,
                    donorEmail,
                    UserPassword);
                subscriptionId = seed.SubscriptionId;

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var subscription = await context.Subscriptions
                    .Include(value => value.InitialDonation)
                    .SingleAsync(value => value.Id == subscriptionId);
                subscription.EasyPaySubscriptionId = easyPaySubscriptionId;
                subscription.InitialDonation.DonationDate = DateTime.UtcNow;
                subscription.InitialDonation.PaymentStatus = PaymentStatus.WaitingPayment;
                var payment = new CreditCardPayment
                {
                    Created = DateTime.UtcNow,
                    EasyPayPaymentId = easyPayPaymentId,
                    TransactionKey = subscription.TransactionKey,
                    Status = "pending",
                    Donation = subscription.InitialDonation,
                };
                subscription.InitialDonation.PaymentList = new List<BasePayment> { payment };
                context.CreditCardPayments.Add(payment);
                await context.SaveChangesAsync();

                var persistedDonation = await context.Donations
                    .Include(value => value.PaymentList)
                    .SingleAsync(value => value.Id == subscription.InitialDonation.Id);
                Assert.Equal(
                    easyPayPaymentId,
                    ((EasyPayBaseClass)persistedDonation.PaymentList.Single()).EasyPayPaymentId);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                webFactory,
                donorEmail,
                UserPassword);

            var response = await client.GetAsync(
                $"/Identity/Account/Manage/Subscriptions/Details?id={subscriptionId}&handler=DataTableData");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"EasyPayPaymentStatus\":\"Payed\"", json);
            Assert.Contains("\"Consistency\":\"Mismatch\"", json);
            Assert.Contains("\"MismatchCount\":1", json);
        }
    }
}
