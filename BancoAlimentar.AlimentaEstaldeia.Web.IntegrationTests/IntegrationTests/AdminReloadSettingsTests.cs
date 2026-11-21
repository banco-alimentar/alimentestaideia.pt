// -----------------------------------------------------------------------
// <copyright file="AdminReloadSettingsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Xunit;

    /// <summary>
    /// Integration tests for admin cache reload.
    /// </summary>
    public class AdminReloadSettingsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string SuperAdminEmail = "integration-superadmin@test.com";
        private const string SuperAdminPassword = IntegrationTestCredentials.DefaultPassword;
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
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, SuperAdminEmail, SuperAdminPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                SuperAdminEmail,
                SuperAdminPassword);

            var response = await client.GetAsync("/Admin/ReloadSettings");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Reload Runtime Settings", html);
            Assert.Contains("Reload settings", html);
            Assert.Contains("Runtime status (this instance)", html);
            Assert.Contains("Cache generation", html);
            Assert.Contains("Last successful asset refresh", html);
        }

        /// <summary>
        /// Authenticated admin can POST reload settings and sees the confirmation message.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_ReloadsSettingsAndShowsMessage_WhenAuthenticatedAsAdmin()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, SuperAdminEmail, SuperAdminPassword);
            }

            var client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                SuperAdminEmail,
                SuperAdminPassword);

            using var configurationScope = this.factory.Services.CreateScope();
            var tenantConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Test:Setting", "cached" },
            }).Build();
            var manager = new KeyVaultConfigurationManager(
                configurationScope.ServiceProvider.GetRequiredService<InfrastructureDbContext>(),
                configurationScope.ServiceProvider.GetRequiredService<IWebHostEnvironment>(),
                configurationScope.ServiceProvider.GetRequiredService<TelemetryClient>(),
                configurationScope.ServiceProvider.GetRequiredService<KeyVaultConfigurationCache>(),
                tenantConfiguration,
                NullLogger<KeyVaultConfigurationManager>.Instance);
            const int cachedTenantId = -1;
            Assert.True(await manager.EnsureTenantConfigurationLoaded(cachedTenantId, new TenantDevelopmentOptions { UseSecrets = true }));
            Assert.NotNull(manager.GetTenantConfiguration(cachedTenantId));

            var getResponse = await client.GetAsync("/Admin/ReloadSettings");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var reloadForm = content.QuerySelector("form[method='post'] button.btn-primary")?.Closest("form");
            Assert.NotNull(reloadForm);
            var postResponse = await client.SendAsync(
                (IHtmlFormElement)reloadForm,
                new Dictionary<string, string>());

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("In-memory settings cache cleared", html);
            Assert.Null(manager.GetTenantConfiguration(cachedTenantId));
        }
    }
}
