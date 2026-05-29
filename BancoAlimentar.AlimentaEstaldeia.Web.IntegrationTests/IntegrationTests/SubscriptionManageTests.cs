// -----------------------------------------------------------------------
// <copyright file="SubscriptionManageTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
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
    }
}
