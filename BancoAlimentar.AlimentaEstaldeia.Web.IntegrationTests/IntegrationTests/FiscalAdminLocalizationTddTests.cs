// -----------------------------------------------------------------------
// <copyright file="FiscalAdminLocalizationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines the Portuguese localization contract for fiscal administration pages.
    /// </summary>
    public sealed class FiscalAdminLocalizationTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "fiscal-localization-admin@integration.test";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdminLocalizationTddTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public FiscalAdminLocalizationTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// A Portuguese (Portugal) administrator sees Portuguese fiscal navigation in the administration home page.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task AdminHome_RendersFiscalNavigationInPortuguese_WhenPortuguesePortugalCultureIsSelected()
        {
            IHtmlDocument document = await this.GetPortugueseDocumentAsync("/Admin");
            string text = document.DocumentElement.TextContent;

            Assert.Contains("Configure os emitentes fiscais e as séries de documentos comunicadas", text, StringComparison.Ordinal);
            Assert.Contains("Perfis de emitente", text, StringComparison.Ordinal);
            Assert.Contains("Séries fiscais", text, StringComparison.Ordinal);
            Assert.Contains("Exportação SAF-T", text, StringComparison.Ordinal);
            Assert.Contains("Documentos fiscais", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Issuer profiles", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Fiscal series", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SAF-T export", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Fiscal documents", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Every fiscal administration screen uses Portuguese text when the request culture is Portuguese (Portugal).
        /// </summary>
        /// <param name="path">The fiscal administration route.</param>
        /// <param name="expectedText">Text that must be visible in Portuguese.</param>
        /// <param name="englishText">The former English text that must not be visible.</param>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Theory]
        [InlineData("/Admin/Fiscal/IssuerProfiles", "Perfis de emitente fiscal", "Fiscal issuer profiles")]
        [InlineData("/Admin/Fiscal/Series", "Séries fiscais", "Fiscal series")]
        [InlineData("/Admin/Fiscal/SaftExport", "Exportação SAF-T", "SAF-T export")]
        [InlineData("/Admin/Fiscal/Documents", "Documentos fiscais", "Fiscal documents")]
        [InlineData("/Admin/Fiscal/Cancellation", "Anular documento fiscal", "Cancel fiscal document")]
        [InlineData("/Admin/Fiscal/Correction", "Corrigir documento fiscal", "Correct fiscal document")]
        public async Task FiscalAdministrationPage_RendersPortugueseText_WhenPortuguesePortugalCultureIsSelected(
            string path,
            string expectedText,
            string englishText)
        {
            IHtmlDocument document = await this.GetPortugueseDocumentAsync(path);
            string text = document.DocumentElement.TextContent;

            Assert.Contains(expectedText, text, StringComparison.Ordinal);
            Assert.DoesNotContain(englishText, text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Fiscal Razor views use localizer lookups and do not retain the original English user-facing literals.
        /// </summary>
        [Fact]
        public void FiscalAdministrationRazorViews_UseLocalizationResourcesInsteadOfEnglishLiterals()
        {
            IReadOnlyDictionary<string, string[]> literalsByView = new Dictionary<string, string[]>
            {
                ["IssuerProfiles.cshtml"] =
                [
                    "Fiscal issuer profiles",
                    "Configure only the issuer information",
                    "Create issuer profile",
                ],
                ["Series.cshtml"] =
                [
                    "Fiscal series",
                    "Record the AT communication reference",
                    "Configure fiscal series",
                    "Close a fiscal series",
                    "Close this fiscal series? It cannot be reopened.",
                ],
                ["SaftExport.cshtml"] =
                [
                    "SAF-T export",
                    "Export the selected issuer's fiscal documents",
                    "Export SAF-T (PT)",
                ],
                ["Documents.cshtml"] =
                [
                    "Fiscal documents",
                    "Any status",
                    "Search fiscal documents",
                    "Invoice date",
                ],
                ["Cancellation.cshtml"] =
                [
                    "Cancel fiscal document",
                    "Cancellation retains the original document",
                ],
                ["Correction.cshtml"] =
                [
                    "Correct fiscal document",
                    "A correction creates a new",
                    "Create fiscal correction",
                    "Fiscal correction created",
                ],
            };

            foreach (KeyValuePair<string, string[]> item in literalsByView)
            {
                string razor = ReadFiscalRazor(item.Key);

                Assert.Contains("@Localizer[", razor, StringComparison.Ordinal);
                foreach (string englishLiteral in item.Value)
                {
                    Assert.DoesNotContain(englishLiteral, razor, StringComparison.Ordinal);
                }
            }
        }

        private static string ReadFiscalRazor(string fileName)
        {
            string repositoryRoot = FindRepositoryRoot();
            string path = Path.Combine(
                repositoryRoot,
                "BancoAlimentar.AlimentaEstaIdeia.Web",
                "Areas",
                "Admin",
                "Pages",
                "Fiscal",
                fileName);
            return File.ReadAllText(path);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }

            return Assert.IsType<string>(directory?.FullName);
        }

        private async Task<IHtmlDocument> GetPortugueseDocumentAsync(string path)
        {
            HttpClient client = await this.CreatePortugueseAdminClientAsync();
            HttpResponseMessage response = await client.GetAsync(path);
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            response.EnsureSuccessStatusCode();
            return document;
        }

        private async Task<HttpClient> CreatePortugueseAdminClientAsync()
        {
            using (IServiceScope scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    IntegrationTestCredentials.DefaultPassword);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                IntegrationTestCredentials.DefaultPassword);
            client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c=pt-PT|uic=pt-PT");
            client.DefaultRequestHeaders.Add("Accept-Language", "pt-PT");
            return client;
        }
    }
}
