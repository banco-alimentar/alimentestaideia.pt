// -----------------------------------------------------------------------
// <copyright file="AdminReloadSettingsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for admin cache reload.
    /// </summary>
    public class AdminReloadSettingsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "integration-admin@test.com";
        private const string AdminPassword = "Test@12345!";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminReloadSettingsTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminReloadSettingsTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
            using var scope = factory.Services.CreateScope();
            IntegrationTestDataSeeder.EnsureAdminUserAsync(scope.ServiceProvider, AdminEmail, AdminPassword).Wait();
        }

        /// <summary>
        /// Anonymous users are redirected to login.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RedirectsToLogin_WhenNotAuthenticated()
        {
            var client = this.factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.GetAsync("/Admin/ReloadSettings");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Identity/Account/Login", response.Headers.Location?.ToString());
        }

        /// <summary>
        /// Admin can clear tenant and memory caches.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_ClearsCache_WhenAuthenticatedAsAdmin()
        {
            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                AdminPassword);

            var getResponse = await client.GetAsync("/Admin/ReloadSettings");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var postResponse = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>());

            Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
            Assert.Contains("/Admin/ReloadSettings", postResponse.Headers.Location?.ToString());

            var followUp = await client.GetAsync(postResponse.Headers.Location);
            followUp.EnsureSuccessStatusCode();
            var html = await followUp.Content.ReadAsStringAsync();
            Assert.Contains("cache cleared", html, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
