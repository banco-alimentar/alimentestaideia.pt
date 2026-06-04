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
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the claim-invoice page.
    /// </summary>
    public class ClaimInvoiceTests : IClassFixture<CustomWebApplicationFactory>
    {
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
                IntegrationTestDataSeeder.AttachInvoiceToDonation(scope.ServiceProvider, donation);
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
            var webFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    IntegrationTestMailConfiguration.AddTrackedStubMail(services);
                });
            });

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(scope.ServiceProvider, publicId);
            }

            var client = webFactory.CreateClient();

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
            Assert.DoesNotContain("asp-for=\"Address\"", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Invalid NIF on POST keeps the claim form visible with validation errors.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_KeepsClaimForm_WhenNifIsInvalid()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(scope.ServiceProvider, publicId);
            }

            var client = this.factory.CreateClient();
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
                    ["Nif"] = "123",
                    ["AcceptsTerms"] = "true",
                });

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("id=\"submit\"", html);
            Assert.Contains("field-validation-error", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// POST with an unknown public id shows the wrong-public-id message without model-binding errors.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_SetsWrongPublicId_WhenDonationNotFound()
        {
            var unknownPublicId = Guid.NewGuid();
            var client = this.factory.CreateClient();
            var getResponse = await client.GetAsync($"/ClaimInvoice?publicId={unknownPublicId}");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);

            var postResponse = await client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[method='post']"),
                new Dictionary<string, string>
                {
                    ["PublicId"] = unknownPublicId.ToString(),
                    ["Address"] = "Rua Integração",
                    ["PostalCode"] = "1000-001",
                    ["Nif"] = "196807050",
                    ["AcceptsTerms"] = "true",
                });

            postResponse.EnsureSuccessStatusCode();
            var html = await postResponse.Content.ReadAsStringAsync();
            Assert.Contains("error processing your Invoice", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("id=\"submit\"", html);
        }

        /// <summary>
        /// Canceled invoice on GET shows the claim form again so the donor can re-submit details.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_ReturnsClaimForm_WhenInvoiceIsCanceled()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                var donation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    publicId,
                    wantsReceipt: true);
                var invoice = IntegrationTestDataSeeder.AttachInvoiceToDonation(scope.ServiceProvider, donation);
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                invoice.IsCanceled = true;
                context.SaveChanges();
            }

            var client = this.factory.CreateClient();
            var response = await client.GetAsync($"/ClaimInvoice?publicId={publicId}");

            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("id=\"submit\"", html);
        }

        /// <summary>
        /// Anonymous callers cannot download invoices using only the public donation id.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GenerateInvoice_RejectsBarePublicDonationIdForAnonymousCaller()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                var donation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    publicId,
                    wantsReceipt: true);
                IntegrationTestDataSeeder.AttachInvoiceToDonation(scope.ServiceProvider, donation);
            }

            var client = this.factory.CreateClient();
            var response = await client.GetAsync(
                $"/Identity/Account/Manage/GenerateInvoice?publicDonationId={publicId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Signed invoice download tokens allow anonymous access until they expire.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GenerateInvoice_AllowsValidSignedToken()
        {
            var publicId = Guid.NewGuid();
            using (var scope = this.factory.Services.CreateScope())
            {
                var donation = await IntegrationTestDataSeeder.SeedPaidDonationWithoutInvoiceAsync(
                    scope.ServiceProvider,
                    publicId,
                    wantsReceipt: true);
                IntegrationTestDataSeeder.AttachInvoiceToDonation(scope.ServiceProvider, donation);
            }

            string token;
            using (var scope = this.factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<IInvoiceDownloadTokenService>();
                token = tokenService.CreateToken(publicId);
            }

            var client = this.factory.CreateClient();
            var response = await client.GetAsync(
                $"/Identity/Account/Manage/GenerateInvoice?token={Uri.EscapeDataString(token)}");

            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Forged or tampered invoice tokens are rejected.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task GenerateInvoice_RejectsInvalidToken()
        {
            var client = this.factory.CreateClient();
            var response = await client.GetAsync(
                "/Identity/Account/Manage/GenerateInvoice?token=definitely-not-a-valid-token");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
