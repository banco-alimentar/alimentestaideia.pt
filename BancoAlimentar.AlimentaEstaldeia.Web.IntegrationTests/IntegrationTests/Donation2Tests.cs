// -----------------------------------------------------------------------
// <copyright file="Donation2Tests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Integration tests for the isolated Donation2 flow.
    /// </summary>
    public class Donation2Tests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory factory;
        private readonly HttpClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="Donation2Tests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public Donation2Tests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
            this.client = factory.CreateClient();
        }

        /// <summary>
        /// Donation2 renders catalogue-driven inputs without legacy Donation selectors.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Get_RendersDynamicProducts_WithIsolatedFormSelectors()
        {
            var response = await this.client.GetAsync("/Donation2");
            response.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(response);

            Assert.NotNull(document.QuerySelector("form#donation2-form"));
            Assert.Null(document.QuerySelector("form#donationForm"));
            Assert.Null(document.QuerySelector("#field1"));
            Assert.NotEmpty(document.QuerySelectorAll("[data-donation2-product]"));
            Assert.NotNull(document.QuerySelector("input[name='Input.Items[0].ProductId']"));
            Assert.NotNull(document.QuerySelector("input[name='Input.Items[0].Quantity']"));
            Assert.NotNull(document.QuerySelector("output[data-donation2-total]"));
            Assert.DoesNotContain("EscolhaOsBensQueQuerDoar", document.Body.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("DescricaoDoCabaz", document.Body.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("¤", document.Body.TextContent, StringComparison.Ordinal);
            Assert.Contains("€", document.Body.TextContent, StringComparison.Ordinal);
        }

        /// <summary>
        /// An anonymous donor can submit a catalogue item and continue to the existing payment page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_AnonymousDonation_CreatesDonationAndRedirectsToPayment()
        {
            var page = await this.client.GetAsync("/Donation2");
            page.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(page);
            var form = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form#donation2-form"));
            var submit = Assert.IsAssignableFrom<IHtmlElement>(document.QuerySelector("button#donation2-submit"));
            var email = $"donation2-{Guid.NewGuid():N}@integration.test";

            var response = await this.client.SendAsync(
                form,
                submit,
                new Dictionary<string, string>
                {
                    ["Input.Items[0].Quantity"] = "1",
                    ["Input.FoodBankId"] = "1",
                    ["Input.Name"] = "Donation2 Donor",
                    ["Input.CompanyName"] = string.Empty,
                    ["Input.Email"] = email,
                    ["Input.Country"] = "Portugal",
                    ["Input.WantsReceipt"] = "false",
                    ["Input.AcceptsTerms"] = "true",
                });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            Assert.True(
                string.Equals("/Payment", response.RequestMessage?.RequestUri?.AbsolutePath, StringComparison.Ordinal),
                await response.Content.ReadAsStringAsync());

            using var scope = this.factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            WebUser user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            var donations = scope.ServiceProvider.GetRequiredService<DonationRepository>().GetUserDonation(user.Id);
            Assert.Single(donations);
            Assert.True(donations.Single().DonationAmount > 0);
        }

        /// <summary>
        /// Product identifiers not present in the current catalogue are rejected server-side.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Post_UnknownProduct_DoesNotCreateDonation()
        {
            var page = await this.client.GetAsync("/Donation2");
            page.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(page);
            var form = Assert.IsAssignableFrom<IHtmlFormElement>(document.QuerySelector("form#donation2-form"));
            var submit = Assert.IsAssignableFrom<IHtmlElement>(document.QuerySelector("button#donation2-submit"));
            var email = $"donation2-invalid-{Guid.NewGuid():N}@integration.test";

            var response = await this.client.SendAsync(
                form,
                submit,
                new Dictionary<string, string>
                {
                    ["Input.Items[0].ProductId"] = "999999",
                    ["Input.Items[0].Quantity"] = "1",
                    ["Input.FoodBankId"] = "1",
                    ["Input.Name"] = "Invalid Donation2 Donor",
                    ["Input.Email"] = email,
                    ["Input.Country"] = "Portugal",
                    ["Input.WantsReceipt"] = "false",
                    ["Input.AcceptsTerms"] = "true",
                });

            response.EnsureSuccessStatusCode();
            Assert.Equal("/Donation2", response.RequestMessage?.RequestUri?.AbsolutePath);
            var resultDocument = await HtmlHelpers.GetDocumentAsync(response);
            Assert.False(string.IsNullOrWhiteSpace(resultDocument.QuerySelector("[data-valmsg-for='Input.Items']")?.TextContent));

            using var scope = this.factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
            Assert.Null(await userManager.FindByEmailAsync(email));
        }
    }
}