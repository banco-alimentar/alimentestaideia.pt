// -----------------------------------------------------------------------
// <copyright file="FiscalPageGuidanceTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using AngleSharp.Dom;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines the user-facing guidance contract for fiscal administration pages.
    /// </summary>
    public sealed class FiscalPageGuidanceTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "fiscal-guidance-admin@integration.test";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalPageGuidanceTddTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public FiscalPageGuidanceTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Every fiscal administration page presents a Portuguese guidance header that explains its purpose and use.
        /// </summary>
        /// <param name="path">The fiscal administration route.</param>
        /// <param name="expectedTitle">The expected page title.</param>
        /// <param name="expectedInstruction">An instruction that a Portuguese administrator can act on.</param>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Theory]
        [InlineData("/Admin/Fiscal/IssuerProfiles", "Perfis de emitente fiscal", "Preencha os dados de identificação do emitente")]
        [InlineData("/Admin/Fiscal/Series", "Séries fiscais", "Comunique a série à AT e registe")]
        [InlineData("/Admin/Fiscal/SaftExport", "Exportação SAF-T", "Indique o emitente e o intervalo de datas")]
        [InlineData("/Admin/Fiscal/Documents", "Documentos fiscais", "Utilize os filtros para localizar")]
        [InlineData("/Admin/Fiscal/Cancellation", "Anular documento fiscal", "Indique o identificador do documento e o motivo")]
        [InlineData("/Admin/Fiscal/Correction", "Corrigir documento fiscal", "Indique o documento original, um identificador único e o motivo")]
        public async Task FiscalAdministrationPage_RendersPortugueseGuidanceHeader_WhenPortuguesePortugalCultureIsSelected(
            string path,
            string expectedTitle,
            string expectedInstruction)
        {
            IHtmlDocument document = await this.GetPortugueseDocumentAsync(path);
            IElement guidanceHeader = document.QuerySelector("header.fiscal-page-guidance")
                ?? throw new InvalidOperationException($"The fiscal guidance header was not rendered for '{path}'.");
            string text = guidanceHeader.TextContent;

            Assert.Contains(expectedTitle, text, StringComparison.Ordinal);
            Assert.Contains("Como utilizar esta página", text, StringComparison.Ordinal);
            Assert.Contains(expectedInstruction, text, StringComparison.Ordinal);
            Assert.DoesNotContain("How to use this page", text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Guidance is sourced from localizer resources instead of being embedded as page literals.
        /// </summary>
        /// <param name="pageName">The fiscal Razor page name without its extension.</param>
        /// <param name="expectedInstruction">The Portuguese instruction in the resource.</param>
        [Theory]
        [InlineData("IssuerProfiles", "Preencha os dados de identificação do emitente")]
        [InlineData("Series", "Comunique a série à AT e registe")]
        [InlineData("SaftExport", "Indique o emitente e o intervalo de datas")]
        [InlineData("Documents", "Utilize os filtros para localizar")]
        [InlineData("Cancellation", "Indique o identificador do documento e o motivo")]
        [InlineData("Correction", "Indique o documento original, um identificador único e o motivo")]
        public void FiscalAdministrationGuidance_UsesDedicatedLocalizedResources(
            string pageName,
            string expectedInstruction)
        {
            string razor = ReadFiscalFile(pageName + ".cshtml");
            XElement portugueseResources = LoadFiscalResource(pageName + ".resx");
            XElement englishResources = LoadFiscalResource(pageName + ".en.resx");

            Assert.Contains("<header class=\"fiscal-page-guidance\"", razor, StringComparison.Ordinal);
            Assert.Contains("@Localizer[\"GuidanceTitle\"]", razor, StringComparison.Ordinal);
            Assert.Contains("@Localizer[\"Guidance\"]", razor, StringComparison.Ordinal);
            Assert.Equal("Como utilizar esta página", GetResourceValue(portugueseResources, "GuidanceTitle"));
            Assert.Contains(expectedInstruction, GetResourceValue(portugueseResources, "Guidance"), StringComparison.Ordinal);
            Assert.Equal("How to use this page", GetResourceValue(englishResources, "GuidanceTitle"));
            Assert.NotEmpty(GetResourceValue(englishResources, "Guidance"));
        }

        private static string GetResourceValue(XElement resources, string name)
        {
            XElement resource = resources.Elements("data")
                .SingleOrDefault(element => string.Equals(element.Attribute("name")?.Value, name, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Resource '{name}' was not found.");
            return resource.Element("value")?.Value
                ?? throw new InvalidOperationException($"Resource '{name}' does not have a value.");
        }

        private static XElement LoadFiscalResource(string fileName)
        {
            return XElement.Load(Path.Combine(
                FindRepositoryRoot(),
                "BancoAlimentar.AlimentaEstaIdeia.Web",
                "Resources",
                "Areas",
                "Admin",
                "Pages",
                "Fiscal",
                fileName));
        }

        private static string ReadFiscalFile(string fileName)
        {
            return File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "BancoAlimentar.AlimentaEstaIdeia.Web",
                "Areas",
                "Admin",
                "Pages",
                "Fiscal",
                fileName));
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
