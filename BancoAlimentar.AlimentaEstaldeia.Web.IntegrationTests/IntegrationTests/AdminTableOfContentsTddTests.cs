// -----------------------------------------------------------------------
// <copyright file="AdminTableOfContentsTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Dom;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines the browser contract for the administration home-page table of contents.
    /// </summary>
    public sealed class AdminTableOfContentsTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "admin-toc@integration.test";
        private const string SuperAdminEmail = "super-admin-toc@integration.test";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminTableOfContentsTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared integration host.</param>
        public AdminTableOfContentsTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// A super administrator can use the horizontal Portuguese index to reach every
        /// administration section, including privileged sections.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task AdminHome_RendersHorizontalPortugueseTableOfContentsForSuperAdmin()
        {
            IHtmlDocument document = await this.GetPortugueseAdminDocumentAsync(isSuperAdmin: true);
            IElement tableOfContents = Assert.IsAssignableFrom<IElement>(document.QuerySelector("nav.admin-table-of-contents"));

            Assert.Contains("d-flex", tableOfContents.ClassName, StringComparison.Ordinal);
            Assert.Contains("flex-wrap", tableOfContents.ClassName, StringComparison.Ordinal);

            AssertLinkTargetsHeading(document, "#daily-operations-heading", "Dia a Dia");
            AssertLinkTargetsHeading(document, "#campaigns-heading", "Campanhas");
            AssertLinkTargetsHeading(document, "#fiscal-heading", "Fiscal");
            AssertLinkTargetsHeading(document, "#quality-heading", "Qualidade");
            AssertLinkTargetsHeading(document, "#management-heading", "Gestão");
            AssertLinkTargetsHeading(document, "#webmaster-heading", "WebMaster");
        }

        /// <summary>
        /// A non-super administrator sees only the sections available to their role.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task AdminHome_HidesPrivilegedTableOfContentsLinksForNonSuperAdmin()
        {
            IHtmlDocument document = await this.GetPortugueseAdminDocumentAsync(isSuperAdmin: false);

            AssertLinkTargetsHeading(document, "#daily-operations-heading", "Dia a Dia");
            AssertLinkTargetsHeading(document, "#campaigns-heading", "Campanhas");
            AssertLinkTargetsHeading(document, "#fiscal-heading", "Fiscal");

            IElement tableOfContents = Assert.IsAssignableFrom<IElement>(document.QuerySelector("nav.admin-table-of-contents"));
            Assert.Null(tableOfContents.QuerySelector("a[href='#quality-heading']"));
            Assert.Null(tableOfContents.QuerySelector("a[href='#management-heading']"));
            Assert.Null(tableOfContents.QuerySelector("a[href='#webmaster-heading']"));
            Assert.DoesNotContain("Qualidade", tableOfContents.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Gestão", tableOfContents.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("WebMaster", tableOfContents.TextContent, StringComparison.Ordinal);
        }

        private static void AssertLinkTargetsHeading(IHtmlDocument document, string href, string expectedText)
        {
            IHtmlAnchorElement anchor = Assert.IsAssignableFrom<IHtmlAnchorElement>(
                document.QuerySelector($"nav.admin-table-of-contents a[href='{href}']"));
            Assert.Equal(expectedText, anchor.TextContent.Trim());

            IElement heading = Assert.IsAssignableFrom<IElement>(document.QuerySelector(href));
            Assert.Contains(heading.LocalName, new[] { "h2", "h3" });
        }

        private async Task<IHtmlDocument> GetPortugueseAdminDocumentAsync(bool isSuperAdmin)
        {
            HttpClient client = await this.CreatePortugueseAdminClientAsync(isSuperAdmin);
            HttpResponseMessage response = await client.GetAsync("/Admin");
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            response.EnsureSuccessStatusCode();
            return document;
        }

        private async Task<HttpClient> CreatePortugueseAdminClientAsync(bool isSuperAdmin)
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

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                email,
                IntegrationTestCredentials.DefaultPassword);
            client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c=pt-PT|uic=pt-PT");
            client.DefaultRequestHeaders.Add("Accept-Language", "pt-PT");
            return client;
        }
    }
}
