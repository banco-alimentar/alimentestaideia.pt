// -----------------------------------------------------------------------
// <copyright file="AdminReloadSettingsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for admin cache reload.
    /// </summary>
    public class AdminReloadSettingsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "integration-admin@test.com";
        private const string AdminPassword = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminReloadSettingsTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminReloadSettingsTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
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

            var response = await client.GetAsync("/Admin/ReloadSettings");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }

        /// <summary>
        /// Authenticated admin can open the reload settings page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ReturnsPage_WhenAuthenticatedAsAdmin()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureAdminUserAsync(scope.ServiceProvider, AdminEmail, AdminPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                AdminPassword);

            var response = await client.GetAsync("/Admin/ReloadSettings");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Reload Runtime Settings", html);
            Assert.Contains("Reload settings", html);
        }
    }
}
