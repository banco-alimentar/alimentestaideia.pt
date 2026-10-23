// -----------------------------------------------------------------------
// <copyright file="FiscalIssuerProfilesSuperAdminTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines the privileged-access contract for fiscal issuer profile configuration.
    /// </summary>
    public sealed class FiscalIssuerProfilesSuperAdminTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "fiscal-issuer-profiles-admin@integration.test";
        private const string SuperAdminEmail = "fiscal-issuer-profiles-super-admin@integration.test";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalIssuerProfilesSuperAdminTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared web application factory.</param>
        public FiscalIssuerProfilesSuperAdminTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// A regular administrator can use the fiscal area but cannot discover the privileged
        /// issuer-profile configuration link.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task AdminHome_HidesIssuerProfilesNavigationForRegularAdministrator()
        {
            HttpClient client = await this.CreateClientAsync(isSuperAdmin: false);
            HttpResponseMessage response = await client.GetAsync("/Admin");
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            response.EnsureSuccessStatusCode();
            Assert.Null(document.QuerySelector("a[href='/Admin/Fiscal/IssuerProfiles']"));
            Assert.NotNull(document.QuerySelector("a[href='/Admin/Fiscal/Series']"));
        }

        /// <summary>
        /// A super administrator can discover the privileged issuer-profile configuration link.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task AdminHome_ExposesIssuerProfilesNavigationForSuperAdministrator()
        {
            HttpClient client = await this.CreateClientAsync(isSuperAdmin: true);
            HttpResponseMessage response = await client.GetAsync("/Admin");
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(document.QuerySelector("a[href='/Admin/Fiscal/IssuerProfiles']"));
        }

        /// <summary>
        /// A regular administrator cannot bypass navigation by requesting the privileged page URL.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task IssuerProfilesPage_RejectsRegularAdministrator()
        {
            HttpClient client = await this.CreateClientAsync(isSuperAdmin: false, allowAutoRedirect: false);

            HttpResponseMessage response = await client.GetAsync("/Admin/Fiscal/IssuerProfiles");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(
                "/Account/AccessDenied",
                response.Headers.Location?.OriginalString,
                StringComparison.Ordinal);
        }

        private async Task<HttpClient> CreateClientAsync(bool isSuperAdmin, bool allowAutoRedirect = true)
        {
            string email = isSuperAdmin ? SuperAdminEmail : AdminEmail;
            using (IServiceScope scope = this.factory.Services.CreateScope())
            {
                if (isSuperAdmin)
                {
                    await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(
                        scope.ServiceProvider,
                        email,
                        IntegrationTestCredentials.DefaultPassword);
                }
                else
                {
                    await IntegrationTestDataSeeder.EnsureAdminUserAsync(
                        scope.ServiceProvider,
                        email,
                        IntegrationTestCredentials.DefaultPassword);
                }
            }

            return await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                email,
                IntegrationTestCredentials.DefaultPassword,
                allowAutoRedirect);
        }
    }
}
