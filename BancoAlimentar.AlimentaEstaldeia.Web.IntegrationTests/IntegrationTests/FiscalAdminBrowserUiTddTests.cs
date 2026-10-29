// -----------------------------------------------------------------------
// <copyright file="FiscalAdminBrowserUiTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Dom;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines browser-facing contracts for the fiscal administration area.
    /// </summary>
    public sealed class FiscalAdminBrowserUiTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "fiscal-browser-admin@integration.test";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdminBrowserUiTddTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public FiscalAdminBrowserUiTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// A RoleArea user can discover each fiscal administration page without knowing a private URL.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task AdminHome_ExposesFiscalNavigationForRoleAreaUsers()
        {
            HttpClient client = await this.CreateAdminClientAsync();

            HttpResponseMessage response = await client.GetAsync("/Admin");
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            response.EnsureSuccessStatusCode();
            Assert.Contains(document.QuerySelectorAll("section h2"), heading =>
                string.Equals(heading.TextContent.Trim(), "Fiscal", StringComparison.OrdinalIgnoreCase));
            AssertNavigationLink(document, "/Admin/Fiscal/IssuerProfiles");
            AssertNavigationLink(document, "/Admin/Fiscal/Series");
            AssertNavigationLink(document, "/Admin/Fiscal/SaftExport");
            AssertNavigationLink(document, "/Admin/Fiscal/Documents");
            AssertNavigationLink(document, "/Admin/Fiscal/Cancellation");
            AssertNavigationLink(document, "/Admin/Fiscal/Correction");
        }

        /// <summary>
        /// The issuer page exposes a non-secret browser form instead of a heading-only placeholder.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task IssuerProfilesPage_ExposesIssuerConfigurationControls()
        {
            IHtmlDocument document = await this.GetFiscalPageAsync("/Admin/Fiscal/IssuerProfiles");

            IHtmlFormElement form = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form[action*='handler=Create']"));
            AssertInput(form, "IssuerProfile.LegalName");
            AssertInput(form, "IssuerProfile.LegalAddress");
            AssertInput(form, "IssuerProfile.TaxRegistrationNumber");
            AssertInput(form, "IssuerProfile.SaftDocumentType");
            AssertInput(form, "IssuerProfile.TaxRule");
            AssertInput(form, "IssuerProfile.SoftwareCertificateNumber");
            AssertInput(form, "IssuerProfile.SigningKeyIdentifier");
            AssertInput(form, "IssuerProfile.SigningKeyVersion");
            AssertInput(form, "IssuerProfile.ValidFromUtc");
            AssertInput(form, "IssuerProfile.ScopeKind");
            AssertInput(form, "IssuerProfile.FoodBankId");
            Assert.NotNull(form.QuerySelector("input[name='IssuerProfile.IsActive']"));
            Assert.NotNull(form.QuerySelector("button[type='submit']"));
            Assert.Null(form.QuerySelector("input[type='password']"));
        }

        /// <summary>
        /// The series page exposes AT communication evidence and lifecycle controls.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task SeriesPage_ExposesSeriesConfigurationAndClosureControls()
        {
            IHtmlDocument document = await this.GetFiscalPageAsync("/Admin/Fiscal/Series");

            IHtmlFormElement configureForm = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form[action*='handler=Configure']"));
            AssertInput(configureForm, "Series.FiscalIssuerProfileId");
            AssertInput(configureForm, "Series.SeriesCode");
            AssertInput(configureForm, "Series.SaftDocumentType");
            AssertInput(configureForm, "Series.ProcessingMethod");
            AssertInput(configureForm, "Series.FirstSequence");
            AssertInput(configureForm, "Series.NextSequence");
            AssertInput(configureForm, "Series.AtcudValidationCode");
            AssertInput(configureForm, "Series.AtCommunicationReference");
            AssertInput(configureForm, "Series.StartsAtUtc");
            AssertInput(configureForm, "Series.Status");

            IHtmlFormElement closeForm = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form[action*='handler=Close']"));
            AssertInput(closeForm, "FiscalDocumentSeriesId");
            Assert.NotNull(closeForm.QuerySelector("button[type='submit']"));
        }

        /// <summary>
        /// The SAF-T page makes the issuer and inclusive period explicit before export.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task SaftExportPage_ExposesIssuerAndPeriodControls()
        {
            IHtmlDocument document = await this.GetFiscalPageAsync("/Admin/Fiscal/SaftExport");

            IHtmlFormElement form = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form[action*='handler=Export']"));
            AssertInput(form, "FiscalIssuerProfileId");
            AssertInput(form, "StartDate");
            AssertInput(form, "EndDate");
            Assert.NotNull(form.QuerySelector("button[type='submit']"));
        }

        /// <summary>
        /// The fiscal-document page renders safe query filters and matching document evidence.
        /// </summary>
        /// <returns>A task representing the test.</returns>
        [Fact]
        public async Task DocumentsPage_ExposesFiltersAndDocumentResults()
        {
            int issuerProfileId = await this.SeedIssuerAndDocumentAsync();
            HttpClient client = await this.CreateAdminClientAsync();

            HttpResponseMessage response = await client.GetAsync($"/Admin/Fiscal/Documents?Query.FiscalIssuerProfileId={issuerProfileId}");
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            response.EnsureSuccessStatusCode();
            IHtmlFormElement form = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form[method='get']"));
            AssertInput(form, "Query.FiscalIssuerProfileId");
            AssertInput(form, "Query.FiscalDocumentSeriesId");
            AssertInput(form, "Query.DocumentNumber");
            AssertInput(form, "Query.Atcud");
            AssertInput(form, "Query.DonationId");
            AssertInput(form, "Query.StartDate");
            AssertInput(form, "Query.EndDate");
            AssertInput(form, "Query.Status");

            IHtmlTableElement results = Assert.IsAssignableFrom<IHtmlTableElement>(document.QuerySelector("table"));
            Assert.Contains("FT 2026/1", results.TextContent, StringComparison.Ordinal);
            Assert.Contains("ATCUD-0001", results.TextContent, StringComparison.Ordinal);
        }

        private static void AssertInput(IHtmlFormElement form, string name)
        {
            Assert.NotNull(form.QuerySelector($"input[name='{name}'], select[name='{name}'], textarea[name='{name}']"));
        }

        private static void AssertNavigationLink(IHtmlDocument document, string href)
        {
            Assert.NotNull(document.QuerySelector($"a[href='{href}']"));
        }

        private async Task<HttpClient> CreateAdminClientAsync()
        {
            using (IServiceScope scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(
                    scope.ServiceProvider,
                    AdminEmail,
                    IntegrationTestCredentials.DefaultPassword);
            }

            return await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                IntegrationTestCredentials.DefaultPassword);
        }

        private async Task<IHtmlDocument> GetFiscalPageAsync(string path)
        {
            HttpClient client = await this.CreateAdminClientAsync();
            HttpResponseMessage response = await client.GetAsync(path);
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);
            response.EnsureSuccessStatusCode();
            return document;
        }

        private async Task<int> SeedIssuerAndDocumentAsync()
        {
            using (IServiceScope scope = this.factory.Services.CreateScope())
            {
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                FiscalIssuerProfile issuer = new FiscalIssuerProfile
                {
                    ScopeKind = FiscalScopeKind.Tenant,
                    LegalName = "Fiscal integration issuer",
                    LegalAddress = "Rua de Teste, 1",
                    TaxRegistrationNumber = "501234567",
                    SaftDocumentType = "FT",
                    TaxRule = "ISE",
                    SoftwareCertificateNumber = "1234",
                    SigningKeyIdentifier = "integration-key",
                    SigningKeyVersion = "1",
                    IsActive = true,
                    ValidFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
                };
                context.FiscalIssuerProfiles.Add(issuer);
                await context.SaveChangesAsync();

                FiscalDocumentSeries series = new FiscalDocumentSeries
                {
                    FiscalIssuerProfileId = issuer.Id,
                    SeriesCode = "FT2026",
                    SaftDocumentType = "FT",
                    FirstSequence = 1,
                    NextSequence = 2,
                    AtcudValidationCode = "ATCUD",
                    AtCommunicationReference = "integration-test",
                    StartsAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                    Status = FiscalSeriesStatus.Active,
                };
                context.FiscalDocumentSeries.Add(series);
                await context.SaveChangesAsync();

                context.FiscalDocuments.Add(new FiscalDocument
                {
                    DonationId = 9001,
                    SeriesId = series.Id,
                    FiscalIssuerProfileId = issuer.Id,
                    InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                    Kind = FiscalDocumentKind.Original,
                    Status = FiscalDocumentStatus.Issued,
                    Sequence = 1,
                    DocumentNumber = "FT 2026/1",
                    Atcud = "ATCUD-0001",
                    InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    IssuedAtUtc = DateTimeOffset.UtcNow,
                    SystemEntryDate = DateTimeOffset.UtcNow,
                    Hash = "integration-hash",
                    HashControl = "1",
                    SigningKeyVersion = "1",
                    SoftwareCertificateNumber = issuer.SoftwareCertificateNumber,
                    IssuerTaxRegistrationNumber = issuer.TaxRegistrationNumber,
                });
                await context.SaveChangesAsync();
                return issuer.Id;
            }
        }
    }
}
