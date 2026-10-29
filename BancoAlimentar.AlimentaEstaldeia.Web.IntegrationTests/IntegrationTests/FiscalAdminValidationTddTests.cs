// -----------------------------------------------------------------------
// <copyright file="FiscalAdminValidationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
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
    /// Defines browser-visible validation contracts for fiscal administration input.
    /// </summary>
    public sealed class FiscalAdminValidationTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "fiscal-validation-admin@integration.test";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdminValidationTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared integration host.</param>
        public FiscalAdminValidationTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Invalid fiscal mutations return their original form with browser-visible validation,
        /// rather than invoking a service with an incomplete request and returning HTTP 500.
        /// </summary>
        /// <param name="pagePath">The Razor Page URL.</param>
        /// <param name="handler">The post handler name.</param>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Theory]
        [InlineData("/Admin/Fiscal/IssuerProfiles", "Create")]
        [InlineData("/Admin/Fiscal/Series", "Configure")]
        [InlineData("/Admin/Fiscal/SaftExport", "Export")]
        [InlineData("/Admin/Fiscal/Cancellation", "Cancel")]
        [InlineData("/Admin/Fiscal/Correction", "Correct")]
        public async Task InvalidFiscalPost_ReturnsItsFormWithValidationFeedback(string pagePath, string handler)
        {
            HttpClient client = await this.CreateAdminClientAsync();
            HttpResponseMessage getResponse = await client.GetAsync(pagePath);
            IHtmlDocument getDocument = await HtmlHelpers.GetDocumentAsync(getResponse);
            getResponse.EnsureSuccessStatusCode();

            string antiForgeryToken = getDocument.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
            Assert.False(string.IsNullOrEmpty(antiForgeryToken));

            using var request = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiForgeryToken,
            });
            HttpResponseMessage postResponse = await client.PostAsync($"{pagePath}?handler={handler}", request);
            IHtmlDocument postDocument = await HtmlHelpers.GetDocumentAsync(postResponse);

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            Assert.NotNull(postDocument.QuerySelector($"form[action*='handler={handler}']"));
            Assert.NotNull(postDocument.QuerySelector(".validation-summary-errors, .field-validation-error"));
        }

        /// <summary>
        /// An invalid fiscal-document filter remains on the search page and reports its input
        /// problem instead of silently dropping the filter.
        /// </summary>
        /// <returns>A task representing the asynchronous test operation.</returns>
        [Fact]
        public async Task InvalidFiscalDocumentSearch_ReturnsFormWithValidationFeedback()
        {
            HttpClient client = await this.CreateAdminClientAsync();

            HttpResponseMessage response = await client.GetAsync("/Admin/Fiscal/Documents?Query.FiscalIssuerProfileId=invalid");
            IHtmlDocument document = await HtmlHelpers.GetDocumentAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(document.QuerySelector("form[method='get']"));
            Assert.NotNull(document.QuerySelector(".validation-summary-errors, .field-validation-error"));
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
    }
}
