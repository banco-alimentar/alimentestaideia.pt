// -----------------------------------------------------------------------
// <copyright file="SubscriptionManageTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
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

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var subscription = await context.Subscriptions.AsNoTracking()
                .FirstAsync(s => s.Id == seed.SubscriptionId);
            Assert.True(subscription.IsDeleted);
            Assert.Equal(SubscriptionStatus.Inactive, subscription.Status);
        }
    }
}
