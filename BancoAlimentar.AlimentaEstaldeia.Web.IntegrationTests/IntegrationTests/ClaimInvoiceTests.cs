// -----------------------------------------------------------------------
// <copyright file="ClaimInvoiceTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    /// <summary>
    /// Integration tests for the claim-invoice page.
    /// </summary>
    public class ClaimInvoiceTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string TestPassword = "Test@12345!";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimInvoiceTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public ClaimInvoiceTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Valid public id without invoice shows the claim form.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ReturnsClaimForm_WhenDonationIsPaidAndHasNoInvoice()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(scope.ServiceProvider, publicId);
            }

            var client = this.factory.CreateClient();
            var response = await client.GetAsync($"/ClaimInvoice?publicId={publicId}");

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("id=\"submit\"", html);
            Assert.Contains(publicId.ToString(), html);
        }

        /// <summary>
        /// Valid public id with existing invoice hides the claim form.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_HidesClaimForm_WhenInvoiceAlreadyExists()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                var donation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    publicId,
                    wantsReceipt: true);
                IntegrationTestDataSeeder.CreateInvoiceForDonation(scope.ServiceProvider, donation);
            }

            var client = this.factory.CreateClient();
            var response = await client.GetAsync($"/ClaimInvoice?publicId={publicId}");

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("id=\"submit\"", html);
        }

        /// <summary>
        /// Posting valid claim data updates the donation and shows confirmation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_ClaimsInvoice_WhenDonationIsValid()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(scope.ServiceProvider, publicId);
            }

            var client = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IMail));
                    services.AddScoped<IMail, StubMail>();
                });
            }).CreateClient();

            var getResponse = await client.GetAsync($"/ClaimInvoice?publicId={publicId}");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var postResponse = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["PublicId"] = publicId.ToString(),
                    ["Address"] = "Rua Integração",
                    ["PostalCode"] = "1000-001",
                    ["Nif"] = "196807050",
                    ["AcceptsTerms"] = "true",
                });

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("id=\"submit\"", html);
        }

        /// <summary>
        /// Invalid public id shows an error on post.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_ShowsError_WhenPublicIdIsUnknown()
        {
            var client = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IMail));
                    services.AddScoped<IMail, StubMail>();
                });
            }).CreateClient();

            var publicId = Guid.NewGuid();
            var getResponse = await client.GetAsync($"/ClaimInvoice?publicId={publicId}");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var postResponse = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["PublicId"] = publicId.ToString(),
                    ["Address"] = "Rua Integração",
                    ["PostalCode"] = "1000-001",
                    ["Nif"] = "196807050",
                    ["AcceptsTerms"] = "true",
                });

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("error processing your Invoice", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
